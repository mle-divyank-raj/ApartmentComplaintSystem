using ACLS.SharedKernel;

namespace ACLS.Domain.Identity.Events;

/// <summary>
/// Raised when a new User account is created in the system.
/// Consumed by notification handlers to send welcome communications.
/// </summary>
public sealed record UserRegisteredEvent(
    int UserId,
    Role Role,
    int PropertyId) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
