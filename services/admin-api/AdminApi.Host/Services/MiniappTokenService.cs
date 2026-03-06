using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AdminApi.Host.Services;

public class MiniappJwtOptions
{
    public string Issuer { get; set; } = "ai-fortune-miniapp";

    public string Audience { get; set; } = "ai-fortune-miniapp-clients";

    public string Secret { get; set; } = "replace-with-miniapp-secret";

    public int ExpireMinutes { get; set; } = 120;
}

public class MiniappTokenService(IOptions<MiniappJwtOptions> options)
{
    public const string OpenIdClaim = "miniapp_openid";

    private readonly MiniappJwtOptions _options = options.Value;

    public (string token, DateTime expiresAt) GenerateToken(string openId)
    {
        var expires = DateTime.UtcNow.AddMinutes(_options.ExpireMinutes);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var tokenDescriptor = new JwtSecurityToken(
            _options.Issuer,
            _options.Audience,
            [
                new Claim(OpenIdClaim, openId),
                new Claim(ClaimTypes.NameIdentifier, openId),
                new Claim("token_type", "miniapp")
            ],
            expires: expires,
            signingCredentials: credentials
        );

        return (new JwtSecurityTokenHandler().WriteToken(tokenDescriptor), expires);
    }
}
