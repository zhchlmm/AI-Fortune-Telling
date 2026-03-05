using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AdminApi.Host.Services;

public class JwtOptions
{
    public string Issuer { get; set; } = "ai-fortune";

    public string Audience { get; set; } = "ai-fortune-clients";

    public string Secret { get; set; } = "replace-with-strong-secret";

    public int ExpireMinutes { get; set; } = 120;
}

public class JwtTokenService(IOptions<JwtOptions> options)
{
    private readonly JwtOptions _options = options.Value;

    public (string token, DateTime expiresAt) GenerateToken(string username)
    {
        var expires = DateTime.UtcNow.AddMinutes(_options.ExpireMinutes);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var tokenDescriptor = new JwtSecurityToken(
            _options.Issuer,
            _options.Audience,
            [
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, "Admin")
            ],
            expires: expires,
            signingCredentials: credentials
        );

        return (new JwtSecurityTokenHandler().WriteToken(tokenDescriptor), expires);
    }
}
