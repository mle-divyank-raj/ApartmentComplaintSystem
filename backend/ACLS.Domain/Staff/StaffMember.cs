using ACLS.SharedKernel;
using ACLS.Domain.Staff.Events;

namespace ACLS.Domain.Staff;

/// <summary>
/// A maintenance worker who receives Complaint assignments and resolves issues.
/// Extends Users via a one-to-one FK (UserId). PropertyId resolves via Users.PropertyId.
/// Skills stored as JSON string in DB via JsonStringListConverter.
/// Table: StaffMembers
/// </summary>
public sealed class StaffMember : EntityBase
{
    public int StaffMemberId { get; private set; }
    public int UserId { get; private set; }
    public string? JobTitle { get; private set; }
    public List<string> Skills { get; private set; } = [];
    public StaffState Availability { get; private set; }
    public decimal? AverageRating { get; private set; }
    public DateTime? LastAssignedAt { get; private set; }

    /// <summary>Private parameterless constructor for EF Core.</summary>
    private StaffMember() { }

    /// <summary>
    /// Creates a new StaffMember linked to an existing User.
    /// Skills are free-form strings (e.g. "Plumbing", "Electrical", "HVAC").
    /// </summary>
    public static StaffMember Create(
        int userId,
        string? jobTitle = null,
        List<string>? skills = null)
    {
        Guard.Against.NegativeOrZero(userId, nameof(userId));

        return new StaffMember
        {
            UserId = userId,
            JobTitle = jobTitle,
            Skills = skills ?? [],
            Availability = StaffState.AVAILABLE
        };
    }

    /// <summary>
    /// Sets availability to BUSY and records the assignment timestamp.
    /// Called atomically with Complaint.Assign() in the same DB transaction.
    /// </summary>
    public void MarkBusy()
    {
        Availability = StaffState.BUSY;
        LastAssignedAt = DateTime.UtcNow;
        RaiseDomainEvent(new StaffAvailabilityChangedEvent(StaffMemberId, StaffState.AVAILABLE, StaffState.BUSY));
    }

    /// <summary>
    /// Sets availability to AVAILABLE.
    /// Called atomically with Complaint.Resolve() in the same DB transaction.
    /// </summary>
    public void MarkAvailable()
    {
        var previousState = Availability;
        Availability = StaffState.AVAILABLE;
        RaiseDomainEvent(new StaffAvailabilityChangedEvent(StaffMemberId, previousState, StaffState.AVAILABLE));
    }

    /// <summary>
    /// Updates the StaffMember's availability to any valid state.
    /// Used for manual state changes (ON_BREAK, OFF_DUTY) toggled by the StaffMember.
    /// </summary>
    public void UpdateAvailability(StaffState newState)
    {
        var previousState = Availability;
        Availability = newState;
        RaiseDomainEvent(new StaffAvailabilityChangedEvent(StaffMemberId, previousState, newState));
    }

    /// <summary>
    /// Updates the StaffMember's skill list.
    /// Skills are free-form strings matching categories used in RequiredSkills on Complaints.
    /// </summary>
    public void UpdateSkills(List<string> skills)
    {
        Guard.Against.Null(skills, nameof(skills));
        Skills = skills;
    }

    /// <summary>
    /// Updates the AverageRating computed by ACLS.Worker after a feedback submission.
    /// Rating must be between 0.00 and 5.00.
    /// </summary>
    public void UpdateAverageRating(decimal averageRating)
    {
        if (averageRating < 0 || averageRating > 5)
            throw new ArgumentOutOfRangeException(nameof(averageRating),
                "AverageRating must be between 0.00 and 5.00.");
        AverageRating = averageRating;
    }

    /// <summary>Updates the job title.</summary>
    public void UpdateJobTitle(string? jobTitle) => JobTitle = jobTitle;
}
