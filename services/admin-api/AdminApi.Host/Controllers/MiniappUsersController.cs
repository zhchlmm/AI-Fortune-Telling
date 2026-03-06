using System.Security.Cryptography;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Text;
using System.Text.Json.Nodes;
using AdminApi.Host.Data;
using AdminApi.Host.Models;
using AdminApi.Host.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AdminApi.Host.Controllers;

[ApiController]
[Route("api/v1/miniapp/users")]
public class MiniappUsersController(
    AdminDbContext dbContext,
    IHttpClientFactory httpClientFactory,
    IOptions<WechatMiniappOptions> miniappOptions,
    MiniappTokenService miniappTokenService,
    ILogger<MiniappUsersController> logger) : ControllerBase
{
    private static readonly Regex EmailRegex = new(@"^[^\s@]+@[^\s@]+\.[^\s@]+$", RegexOptions.Compiled);
    private static readonly Regex PhoneRegex = new(@"^1\d{10}$", RegexOptions.Compiled);

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
        var (token, expiresAt) = miniappTokenService.GenerateToken(openId);

        return Ok(new MiniappLoginByCodeResponse(openId, token, expiresAt));
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpGet("profile/me")]
    public async Task<ActionResult<MiniappUserProfileResponse>> GetProfileMe()
    {
        var openId = GetOpenIdFromClaims();
        if (string.IsNullOrWhiteSpace(openId))
        {
            return Unauthorized(new MiniappErrorResponse("UNAUTHORIZED", "用户身份无效，请重新登录"));
        }

        var user = await dbContext.MiniappUsers.FirstOrDefaultAsync(x => x.OpenId == openId, HttpContext.RequestAborted);
        if (user is null)
        {
            return NotFound(new MiniappErrorResponse("USER_NOT_FOUND", "用户不存在"));
        }

        return Ok(ToProfileResponse(user));
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpPut("profile/me")]
    public async Task<ActionResult<MiniappUserProfileResponse>> UpdateProfileMe([FromBody] UpdateMiniappUserProfileRequest request)
    {
        var openId = GetOpenIdFromClaims();
        if (string.IsNullOrWhiteSpace(openId))
        {
            return Unauthorized(new MiniappErrorResponse("UNAUTHORIZED", "用户身份无效，请重新登录"));
        }

        var user = await dbContext.MiniappUsers.FirstOrDefaultAsync(x => x.OpenId == openId, HttpContext.RequestAborted);
        if (user is null)
        {
            return NotFound(new MiniappErrorResponse("USER_NOT_FOUND", "用户不存在"));
        }

        if (user.IsBlocked)
        {
            logger.LogWarning("Miniapp profile update blocked for openId: {OpenId}", openId);
            return StatusCode(403, new MiniappErrorResponse("USER_BLOCKED", "账号已被限制，暂不可修改资料"));
        }

        var nickname = NormalizeOptionalField(request.Nickname);
        var email = NormalizeOptionalField(request.Email);
        var phoneNumber = NormalizeOptionalField(request.PhoneNumber);
        var avatar = NormalizeOptionalField(request.Avatar);

        if (!string.IsNullOrWhiteSpace(email) && !EmailRegex.IsMatch(email))
        {
            return BadRequest(new MiniappErrorResponse("INVALID_EMAIL", "邮箱格式不正确"));
        }

        if (!string.IsNullOrWhiteSpace(phoneNumber) && !PhoneRegex.IsMatch(phoneNumber))
        {
            return BadRequest(new MiniappErrorResponse("INVALID_PHONE_NUMBER", "手机号格式不正确"));
        }

        var changedFields = CollectChangedFields(user, nickname, email, phoneNumber, avatar);
        user.Nickname = nickname;
        user.Email = email;
        user.PhoneNumber = phoneNumber;
        user.Avatar = avatar;
        user.UpdatedAt = DateTime.UtcNow;

        dbContext.MiniappProfileAudits.Add(new MiniappProfileAuditEntity
        {
            Id = Guid.NewGuid(),
            OpenId = openId,
            Action = "profile_update",
            ChangedFields = changedFields,
            CreatedAt = DateTime.UtcNow,
        });

        await dbContext.SaveChangesAsync(HttpContext.RequestAborted);

        return Ok(ToProfileResponse(user));
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpPost("phone")]
    public async Task<ActionResult<MiniappUserProfileResponse>> UpdatePhone([FromBody] UpdateMiniappUserPhoneRequest request)
    {
        var openId = GetOpenIdFromClaims();
        if (string.IsNullOrWhiteSpace(openId))
        {
            return Unauthorized(new MiniappErrorResponse("UNAUTHORIZED", "用户身份无效，请重新登录"));
        }

        if (string.IsNullOrWhiteSpace(request.EncryptedData) || string.IsNullOrWhiteSpace(request.Iv))
        {
            return BadRequest(new MiniappErrorResponse("INVALID_PHONE_PAYLOAD", "encryptedData 和 iv 不能为空"));
        }

        var user = await dbContext.MiniappUsers.FirstOrDefaultAsync(x => x.OpenId == openId, HttpContext.RequestAborted);
        if (user is null)
        {
            return NotFound(new MiniappErrorResponse("USER_NOT_FOUND", "用户不存在"));
        }

        if (user.IsBlocked)
        {
            logger.LogWarning("Miniapp phone update blocked for openId: {OpenId}", openId);
            return StatusCode(403, new MiniappErrorResponse("USER_BLOCKED", "账号已被限制，暂不可修改资料"));
        }

        if (string.IsNullOrWhiteSpace(user.SessionKey))
        {
            logger.LogWarning("Miniapp phone update failed: session key missing for openId: {OpenId}", openId);
            return BadRequest(new MiniappErrorResponse("SESSION_KEY_MISSING", "当前用户缺少 sessionKey，请重新登录"));
        }

        string? phoneNumber;
        try
        {
            phoneNumber = TryDecryptPhoneNumber(request.EncryptedData, request.Iv, user.SessionKey);
        }
        catch
        {
            logger.LogWarning("Miniapp phone decrypt failed for openId: {OpenId}", openId);
            return BadRequest(new MiniappErrorResponse("PHONE_DECRYPT_FAILED", "手机号解析失败"));
        }

        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            return BadRequest(new MiniappErrorResponse("PHONE_DECRYPT_FAILED", "手机号解析失败"));
        }

        var normalizedPhone = NormalizeOptionalField(phoneNumber);
        if (string.IsNullOrWhiteSpace(normalizedPhone) || !PhoneRegex.IsMatch(normalizedPhone))
        {
            return BadRequest(new MiniappErrorResponse("INVALID_PHONE_NUMBER", "手机号格式不正确"));
        }

        var oldPhone = user.PhoneNumber;
        user.PhoneNumber = normalizedPhone;
        user.UpdatedAt = DateTime.UtcNow;

        dbContext.MiniappProfileAudits.Add(new MiniappProfileAuditEntity
        {
            Id = Guid.NewGuid(),
            OpenId = openId,
            Action = "phone_update_from_wechat",
            ChangedFields = $"PhoneNumber: {MaskPhone(oldPhone)} -> {MaskPhone(normalizedPhone)}",
            CreatedAt = DateTime.UtcNow,
        });

        await dbContext.SaveChangesAsync(HttpContext.RequestAborted);

        return Ok(ToProfileResponse(user));
    }

    private string? GetOpenIdFromClaims()
    {
        return User.FindFirstValue(MiniappTokenService.OpenIdClaim)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    private static string CollectChangedFields(
        MiniappUserEntity user,
        string? nickname,
        string? email,
        string? phoneNumber,
        string? avatar)
    {
        var changes = new List<string>();
        if (!string.Equals(user.Nickname, nickname, StringComparison.Ordinal))
        {
            changes.Add($"Nickname: {MaskText(user.Nickname)} -> {MaskText(nickname)}");
        }

        if (!string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
        {
            changes.Add($"Email: {MaskEmail(user.Email)} -> {MaskEmail(email)}");
        }

        if (!string.Equals(user.PhoneNumber, phoneNumber, StringComparison.Ordinal))
        {
            changes.Add($"PhoneNumber: {MaskPhone(user.PhoneNumber)} -> {MaskPhone(phoneNumber)}");
        }

        if (!string.Equals(user.Avatar, avatar, StringComparison.Ordinal))
        {
            changes.Add("Avatar: changed");
        }

        return changes.Count == 0 ? "no_field_changed" : string.Join("; ", changes);
    }

    private static string MaskText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return "<empty>";
        }

        if (text.Length <= 1)
        {
            return "*";
        }

        return $"{text[0]}***";
    }

    private static string MaskEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
        {
            return "<empty>";
        }

        var at = email.IndexOf('@');
        if (at <= 1)
        {
            return $"***{email[at..]}";
        }

        return $"{email[0]}***{email[at..]}";
    }

    private static string MaskPhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone) || phone.Length < 7)
        {
            return "<empty>";
        }

        return $"{phone[..3]}****{phone[^4..]}";
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
