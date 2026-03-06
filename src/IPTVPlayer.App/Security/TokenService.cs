using System.Security.Cryptography;
using System.Text;

namespace IPTVPlayer.App.Security;

public class TokenService
{
    private readonly byte[] _secretKey = RandomNumberGenerator.GetBytes(32);

    public string GenerateToken(string payload, TimeSpan ttl)
    {
        var expires = DateTimeOffset.UtcNow.Add(ttl).ToUnixTimeSeconds();
        var raw = $"{payload}|{expires}";
        using var hmac = new HMACSHA256(_secretKey);
        var signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(raw)));
        return Convert.ToBase64String(Encoding.UTF8.GetBytes($"{raw}|{signature}"));
    }

    public bool ValidateToken(string token)
    {
        try
        {
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(token));
            var parts = decoded.Split('|');
            if (parts.Length < 3)
            {
                return false;
            }

            var payload = parts[0];
            var expiry = long.Parse(parts[1]);
            var signature = parts[2];

            if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > expiry)
            {
                return false;
            }

            var raw = $"{payload}|{expiry}";
            using var hmac = new HMACSHA256(_secretKey);
            var expected = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(raw)));
            return CryptographicOperations.FixedTimeEquals(Convert.FromBase64String(signature), Convert.FromBase64String(expected));
        }
        catch
        {
            return false;
        }
    }
}
