using System.Security.Cryptography;
using System.Text;

namespace IPTVPlayer.App.Security;

public class SecureStorageService
{
    private readonly string _baseDir;

    public SecureStorageService()
    {
        _baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "IPTVPlayer");
        Directory.CreateDirectory(_baseDir);
    }

    public async Task SaveSecretAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        var encrypted = ProtectedData.Protect(Encoding.UTF8.GetBytes(value), null, DataProtectionScope.CurrentUser);
        await File.WriteAllBytesAsync(GetPath(key), encrypted, cancellationToken);
    }

    public async Task<string?> GetSecretAsync(string key, CancellationToken cancellationToken = default)
    {
        var path = GetPath(key);
        if (!File.Exists(path))
        {
            return null;
        }

        var encrypted = await File.ReadAllBytesAsync(path, cancellationToken);
        var decrypted = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
        return Encoding.UTF8.GetString(decrypted);
    }

    private string GetPath(string key) => Path.Combine(_baseDir, $"{key}.bin");
}
