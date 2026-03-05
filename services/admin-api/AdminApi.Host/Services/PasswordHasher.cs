using System.Security.Cryptography;
using System.Text;

namespace AdminApi.Host.Services;

public class PasswordHasher
{
    public string GenerateSalt()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
    }

    public string HashPassword(string password, string salt)
    {
        var bytes = Encoding.UTF8.GetBytes(salt + password);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }

    public bool Verify(string password, string salt, string hash)
    {
        var computed = HashPassword(password, salt);
        return computed == hash;
    }
}
