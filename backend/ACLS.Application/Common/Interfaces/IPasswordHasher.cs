namespace ACLS.Application.Common.Interfaces;

/// <summary>
/// Interface for hashing and verifying passwords.
/// Defined in Application; implemented in ACLS.Infrastructure using BCrypt.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>Hashes a plaintext password. Returns a BCrypt hash string.</summary>
    string Hash(string password);

    /// <summary>Returns true if the plaintext password matches the stored hash.</summary>
    bool Verify(string password, string hash);
}
