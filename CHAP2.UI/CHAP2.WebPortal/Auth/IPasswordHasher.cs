namespace CHAP2.WebPortal.Auth;

/// <summary>
/// One responsibility: hash a password and verify a candidate against
/// an existing hash. Separated from any auth flow so the hashing scheme
/// can be swapped (PBKDF2 -> Argon2 etc.) without touching controllers.
/// </summary>
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string storedHash);
}
