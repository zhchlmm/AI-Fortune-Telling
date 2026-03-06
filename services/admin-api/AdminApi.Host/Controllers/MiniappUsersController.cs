using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using AdminApi.Host.Data;
using AdminApi.Host.Models;
using AdminApi.Host.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AdminApi.Host.Controllers;

[ApiController]
[Route("api/v1/miniapp/users")]
public class MiniappUsersController(
    AdminDbContext dbContext,
    IHttpClientFactory httpClientFactory,
    IOptions<WechatMiniappOptions> miniappOptions) : ControllerBase
{
    private readonly WechatMiniappOptions _options = miniappOptions.Value;

    [HttpPost("login-by-code")]
    public async Task<ActionResult<MiniappLoginByCodeResponse>> LoginByCode([FromBody] MiniappLoginByCodeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
        {
            return BadRequest(new { message = "code 不能为空" });
        }

        var (openId, sessionKey) = await ResolveOpenIdAndSessionKeyAsync(request.Code.Trim(), HttpContext.RequestAborted);
        var now = DateTime.UtcNow;

        var user = await dbContext.MiniappUsers.FirstOrDefaultAsync(x => x.OpenId == openId, HttpContext.RequestAborted);
        if (user is null)
        {
            user = new MiniappUserEntity
            {
                Id = Guid.NewGuid(),
                OpenId = openId,
                SessionKey = sessionKey,
                CreatedAt = now,
                UpdatedAt = now,
            };
            dbContext.MiniappUsers.Add(user);
        }
        else
        {
            user.SessionKey = sessionKey;
            user.UpdatedAt = now;
        }

        await dbContext.SaveChangesAsync(HttpContext.RequestAborted);

        return Ok(new MiniappLoginByCodeResponse(openId));
    }

    [HttpGet("profile")]
    public async Task<ActionResult<MiniappUserProfileResponse>> GetProfile([FromQuery] string openId)
    {
        if (string.IsNullOrWhiteSpace(openId))
        {
            return BadRequest(new { message = "openId 不能为空" });
        }

        var user = await dbContext.MiniappUsers.FirstOrDefaultAsync(x => x.OpenId == openId, HttpContext.RequestAborted);
        if (user is null)
        {
            return NotFound(new { message = "用户不存在" });
        }

        return Ok(ToProfileResponse(user));
    }

    [HttpPut("profile")]
    public async Task<ActionResult<MiniappUserProfileResponse>> UpdateProfile([FromBody] UpdateMiniappUserProfileRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.OpenId))
        {
            return BadRequest(new { message = "openId 不能为空" });
        }

        var user = await dbContext.MiniappUsers.FirstOrDefaultAsync(x => x.OpenId == request.OpenId, HttpContext.RequestAborted);
        if (user is null)
        {
            return NotFound(new { message = "用户不存在" });
        }

        user.Nickname = NormalizeOptionalField(request.Nickname);
        user.Email = NormalizeOptionalField(request.Email);
        user.PhoneNumber = NormalizeOptionalField(request.PhoneNumber);
        user.Avatar = NormalizeOptionalField(request.Avatar);
        user.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(HttpContext.RequestAborted);

        return Ok(ToProfileResponse(user));
    }

    [HttpPost("phone")]
    public async Task<ActionResult<MiniappUserProfileResponse>> UpdatePhone([FromBody] UpdateMiniappUserPhoneRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.OpenId))
        {
            return BadRequest(new { message = "openId 不能为空" });
        }

        if (string.IsNullOrWhiteSpace(request.EncryptedData) || string.IsNullOrWhiteSpace(request.Iv))
        {
            return BadRequest(new { message = "encryptedData 和 iv 不能为空" });
        }

        var user = await dbContext.MiniappUsers.FirstOrDefaultAsync(x => x.OpenId == request.OpenId, HttpContext.RequestAborted);
        if (user is null)
        {
            return NotFound(new { message = "用户不存在" });
        }

        if (string.IsNullOrWhiteSpace(user.SessionKey))
        {
            return BadRequest(new { message = "当前用户缺少 sessionKey，请重新登录" });
        }

        string? phoneNumber;
        try
        {
            phoneNumber = TryDecryptPhoneNumber(request.EncryptedData, request.Iv, user.SessionKey);
        }
        catch
        {
            return BadRequest(new { message = "手机号解析失败" });
        }

        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            return BadRequest(new { message = "手机号解析失败" });
        }

        user.PhoneNumber = phoneNumber;
        user.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(HttpContext.RequestAborted);

        return Ok(ToProfileResponse(user));
    }

    private async Task<(string openId, string sessionKey)> ResolveOpenIdAndSessionKeyAsync(string code, CancellationToken cancellationToken)
    {
        // 开发环境未配置微信凭据时，提供可联调的稳定 openId。
        if (string.IsNullOrWhiteSpace(_options.AppId) || string.IsNullOrWhiteSpace(_options.AppSecret))
        {
            var fakeOpenId = $"dev-{code}";
            var fakeSessionKey = Convert.ToBase64String(Encoding.UTF8.GetBytes($"session-{code}").Take(16).ToArray());
            return (fakeOpenId, fakeSessionKey);
        }

        var client = httpClientFactory.CreateClient();
        var url =
            $"https://api.weixin.qq.com/sns/jscode2session?appid={Uri.EscapeDataString(_options.AppId)}&secret={Uri.EscapeDataString(_options.AppSecret)}&js_code={Uri.EscapeDataString(code)}&grant_type=authorization_code";

        var response = await client.GetAsync(url, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException("调用微信登录接口失败");
        }

        var json = JsonNode.Parse(payload)?.AsObject();
        if (json is null)
        {
            throw new InvalidOperationException("微信登录响应格式错误");
        }

        if (json.TryGetPropertyValue("errcode", out var errCodeNode) && errCodeNode is not null)
        {
            throw new InvalidOperationException($"微信登录失败: {payload}");
        }

        var openId = json["openid"]?.GetValue<string>();
        var sessionKey = json["session_key"]?.GetValue<string>();

        if (string.IsNullOrWhiteSpace(openId) || string.IsNullOrWhiteSpace(sessionKey))
        {
            throw new InvalidOperationException("微信登录返回缺少 openid/session_key");
        }

        return (openId, sessionKey);
    }

    private static string? TryDecryptPhoneNumber(string encryptedData, string iv, string sessionKey)
    {
        var encryptedBytes = Convert.FromBase64String(encryptedData);
        var ivBytes = Convert.FromBase64String(iv);
        var keyBytes = Convert.FromBase64String(sessionKey);

        using var aes = Aes.Create();
        aes.Key = keyBytes;
        aes.IV = ivBytes;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var decryptor = aes.CreateDecryptor();
        using var ms = new MemoryStream(encryptedBytes);
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var sr = new StreamReader(cs, Encoding.UTF8);

        var decrypted = sr.ReadToEnd();
        var payload = JsonNode.Parse(decrypted)?.AsObject();
        return payload?["phoneNumber"]?.GetValue<string>()
            ?? payload?["purePhoneNumber"]?.GetValue<string>();
    }

    private static string? NormalizeOptionalField(string? value)
    {
        if (value is null)
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length == 0 ? null : trimmed;
    }

    private static MiniappUserProfileResponse ToProfileResponse(MiniappUserEntity user)
        => new(
            user.OpenId,
            user.Nickname,
            user.Avatar,
            user.Email,
            user.PhoneNumber,
            user.UpdatedAt
        );
}
