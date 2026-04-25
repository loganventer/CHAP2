using System.Security.Cryptography;
using System.Text;

namespace CHAP2.WebPortal.Auth;

/// <summary>
/// PBKDF2-SHA256 hasher. Self-describing string format:
///   pbkdf2-sha256$&lt;iterations&gt;$&lt;saltBase64&gt;$&lt;keyBase64&gt;
/// Iterations and salt live inside the hash, so changing the work
/// factor or salt size doesn't invalidate existing hashes.
///
/// Single responsibility: hash a password string and verify a
/// candidate against a stored hash. No knowledge of users, sessions
/// or HTTP. Constant-time comparison via CryptographicOperations.
/// </summary>
public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const int DefaultIterations = 100_000;
    private const int SaltSizeBytes = 16;
    private const int KeySizeBytes = 32;

    public string Hash(string password)
    {
        if (password is null) throw new ArgumentNullException(nameof(password));

        var salt = RandomNumberGenerator.GetBytes(SaltSizeBytes);
        var key = Pbkdf2(password, salt, DefaultIterations, KeySizeBytes);
        return $"pbkdf2-sha256${DefaultIterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(key)}";
    }

    public bool Verify(string password, string storedHash)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(storedHash)) return false;

        var parts = storedHash.Split('$');
        if (parts.Length != 4 || parts[0] != "pbkdf2-sha256") return false;
        if (!int.TryParse(parts[1], out var iterations) || iterations <= 0) return false;

        byte[] salt, expectedKey;
        try
        {
            salt = Convert.FromBase64String(parts[2]);
            expectedKey = Convert.FromBase64String(parts[3]);
        }
        catch (FormatException)
        {
            return false;
        }

        var actualKey = Pbkdf2(password, salt, iterations, expectedKey.Length);
        return CryptographicOperations.FixedTimeEquals(actualKey, expectedKey);
    }

    private static byte[] Pbkdf2(string password, byte[] salt, int iterations, int keyBytes)
    {
        using var pbkdf2 = new Rfc2898DeriveBytes(
            Encoding.UTF8.GetBytes(password),
            salt,
            iterations,
            HashAlgorithmName.SHA256);
        return pbkdf2.GetBytes(keyBytes);
    }
}
