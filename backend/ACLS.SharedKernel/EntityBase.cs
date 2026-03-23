namespace ACLS.SharedKernel;

/// <summary>
/// Abstract base class for all domain entities in ACLS.Domain.
/// Provides the domain events collection mechanism.
/// All entities inherit from this class — never from each other.
///
/// EF Core Note: Each entity that inherits EntityBase has its own named primary key
/// property (e.g. ComplaintId, StaffMemberId). EF Core entity configurations must
/// configure that property as the key and ignore the inherited Id if needed.
/// </summary>
public abstract class EntityBase
{
    /// <summary>
    /// Generic integer identity. Each concrete entity exposes its own named PK
    /// (e.g. ComplaintId) and EF Core configurations use HasKey() on that property.
    /// This property provides a uniform identity accessor for generic domain utilities.
    /// </summary>
    public int Id { get; protected set; }

    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>
    /// Domain events raised by this entity since it was loaded or created.
    /// Published by command handlers AFTER the transaction commits.
    /// </summary>
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Appends a domain event to the entity's internal collection.
    /// Called from within entity state-change methods.
    /// </summary>
    protected void RaiseDomainEvent(IDomainEvent domainEvent)
        => _domainEvents.Add(domainEvent);

    /// <summary>
    /// Removes all domain events from the collection.
    /// Called by the command handler after publishing all events.
    /// </summary>
    public void ClearDomainEvents() => _domainEvents.Clear();
}
