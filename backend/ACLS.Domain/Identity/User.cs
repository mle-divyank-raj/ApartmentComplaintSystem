using ACLS.SharedKernel;
using ACLS.Domain.Identity.Events;

namespace ACLS.Domain.Identity;

/// <summary>
/// Base user entity. All system actors (Residents, Managers, Maintenance Staff) have
/// a row in the Users table. Role-specific data lives in Residents and StaffMembers extension tables.
/// Table: Users
/// </summary>
public sealed class User : EntityBase
{
    public int UserId { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string? Phone { get; private set; }
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public Role Role { get; private set; }
    public int PropertyId { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastLoginAt { get; private set; }

    /// <summary>Private parameterless constructor for EF Core.</summary>
    private User() { }

    /// <summary>
    /// Creates a new User entity. PasswordHash must be a BCrypt hash — never a plaintext password.
    /// </summary>
    public static User Create(
        string email,
        string passwordHash,
        string firstName,
        string lastName,
        Role role,
        int propertyId,
        string? phone = null)
    {
        Guard.Against.NullOrWhiteSpace(email, nameof(email));
        Guard.Against.NullOrWhiteSpace(passwordHash, nameof(passwordHash));
        Guard.Against.NullOrWhiteSpace(firstName, nameof(firstName));
        Guard.Against.NullOrWhiteSpace(lastName, nameof(lastName));
        Guard.Against.NegativeOrZero(propertyId, nameof(propertyId));

        var user = new User
        {
            Email = email.ToLowerInvariant(),
            PasswordHash = passwordHash,
            FirstName = firstName,
            LastName = lastName,
            Role = role,
            PropertyId = propertyId,
            Phone = phone,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        user.RaiseDomainEvent(new UserRegisteredEvent(user.UserId, role, propertyId));
        return user;
    }

    /// <summary>Deactivates the user account. Deactivated users cannot log in.</summary>
    public void Deactivate() => IsActive = false;

    /// <summary>Reactivates a previously deactivated user account.</summary>
    public void Reactivate() => IsActive = true;

    /// <summary>Records the UTC timestamp of a successful login.</summary>
    public void RecordLogin() => LastLoginAt = DateTime.UtcNow;

    /// <summary>Updates the BCrypt password hash. Never accepts a plaintext password.</summary>
    public void UpdatePasswordHash(string newPasswordHash)
    {
        Guard.Against.NullOrWhiteSpace(newPasswordHash, nameof(newPasswordHash));
        PasswordHash = newPasswordHash;
    }

    /// <summary>Updates the user's phone number.</summary>
    public void UpdatePhone(string? phone) => Phone = phone;

    /// <summary>Full display name: FirstName + " " + LastName.</summary>
    public string FullName => $"{FirstName} {LastName}";
}
