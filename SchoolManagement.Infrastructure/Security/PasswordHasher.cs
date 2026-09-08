using System.Security.Cryptography;
using SchoolManagement.Application.Interfaces;

namespace SchoolManagement.Infrastructure.Security;

/// <summary>
/// PBKDF2 (HMAC-SHA256) password hashing.
/// </summary>
/// <remarks>
/// The stored value is self describing: "PBKDF2-SHA256.iterations.salt.key", all
/// parts Base64 where relevant. Embedding the parameters means the work factor can
/// be raised later without invalidating existing passwords.
/// Verification uses a fixed time comparison to avoid leaking information.
/// </remarks>
public class PasswordHasher : IPasswordHasher
{
    private const string Algorithm = "PBKDF2-SHA256";
    private const int DefaultIterations = 210_000;
    private const int SaltSize = 16;
    private const int KeySize = 32;

    public string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, DefaultIterations, HashAlgorithmName.SHA256, KeySize);

        return string.Join('.', Algorithm, DefaultIterations, Convert.ToBase64String(salt), Convert.ToBase64String(key));
    }

    public bool Verify(string password, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(passwordHash))
        {
            return false;
        }

        var parts = passwordHash.Split('.');
        if (parts.Length != 4 || parts[0] != Algorithm)
        {
            return false;
        }

        if (!int.TryParse(parts[1], out var iterations) || iterations <= 0)
        {
            return false;
        }

        byte[] salt;
        byte[] expectedKey;
        try
        {
            salt = Convert.FromBase64String(parts[2]);
            expectedKey = Convert.FromBase64String(parts[3]);
        }
        catch (FormatException)
        {
            return false;
        }

        var actualKey = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            iterations,
            HashAlgorithmName.SHA256,
            expectedKey.Length);

        return CryptographicOperations.FixedTimeEquals(actualKey, expectedKey);
    }
}
