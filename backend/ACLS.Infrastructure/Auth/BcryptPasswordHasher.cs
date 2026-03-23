using ACLS.Application.Common.Interfaces;
using BCrypt.Net;

namespace ACLS.Infrastructure.Auth;

/// <summary>
/// BCrypt-based password hasher. Work factor 12 provides adequate protection against brute force.
/// </summary>
public sealed class BcryptPasswordHasher : IPasswordHasher
{
    public string Hash(string password) =>
        BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);

    public bool Verify(string password, string hash) =>
        BCrypt.Net.BCrypt.Verify(password, hash);
}
