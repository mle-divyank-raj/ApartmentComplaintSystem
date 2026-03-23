using ACLS.SharedKernel;

namespace ACLS.Domain.AuditLog;

/// <summary>
/// An immutable record of a state-changing action taken by an Actor on a system entity.
/// The Audit Log provides a tamper-evident history for compliance and forensic review.
/// Audit entries are NEVER updated or deleted — only AddAsync is exposed on IAuditRepository.
/// Table: AuditLog
/// </summary>
public sealed class AuditEntry : EntityBase
{
    public int AuditEntryId { get; private set; }
    public int? PropertyId { get; private set; }
    public int? ActorUserId { get; private set; }
    public AuditAction Action { get; private set; }
    public string EntityType { get; private set; } = string.Empty;
    public int EntityId { get; private set; }
    public string? ActorRole { get; private set; }
    public DateTime OccurredAt { get; private set; }
    public string? OldValue { get; private set; }
    public string? NewValue { get; private set; }
    public string? IpAddress { get; private set; }

    /// <summary>Private parameterless constructor for EF Core.</summary>
    private AuditEntry() { }

    /// <summary>
    /// Creates an immutable AuditEntry for a user-initiated action.
    /// PropertyId and ActorUserId are nullable to support system-initiated events.
    /// OldValue and NewValue should be serialised JSON snippets of the changed fields only.
    /// </summary>
    public static AuditEntry Create(
        AuditAction action,
        string entityType,
        int entityId,
        int? propertyId = null,
        int? actorUserId = null,
        string? actorRole = null,
        string? oldValue = null,
        string? newValue = null,
        string? ipAddress = null)
    {
        Guard.Against.NullOrWhiteSpace(entityType, nameof(entityType));

        return new AuditEntry
        {
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            PropertyId = propertyId,
            ActorUserId = actorUserId,
            ActorRole = actorRole,
            OccurredAt = DateTime.UtcNow,
            OldValue = oldValue,
            NewValue = newValue,
            IpAddress = ipAddress
        };
    }
}
