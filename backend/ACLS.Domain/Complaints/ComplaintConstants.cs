namespace ACLS.Domain.Complaints;

/// <summary>
/// Domain-level constants for the Complaints bounded context.
/// All numeric literals related to Complaints must be sourced from this class.
/// </summary>
public static class ComplaintConstants
{
    /// <summary>Maximum number of Media attachments a Resident may upload per Complaint at submission time.</summary>
    public const int MaxMediaAttachments = 3;

    /// <summary>Maximum character length for Complaint Title.</summary>
    public const int MaxTitleLength = 200;

    /// <summary>Maximum character length for Complaint Description.</summary>
    public const int MaxDescriptionLength = 2000;

    /// <summary>Maximum character length for Complaint Category.</summary>
    public const int MaxCategoryLength = 100;

    /// <summary>Maximum character length for ResidentFeedbackComment.</summary>
    public const int MaxFeedbackCommentLength = 1000;

    /// <summary>Maximum character length for the RequiredSkills JSON string stored in the database.</summary>
    public const int MaxRequiredSkillsJsonLength = 500;

    /// <summary>Minimum valid ResidentRating value (inclusive).</summary>
    public const int MinRating = 1;

    /// <summary>Maximum valid ResidentRating value (inclusive).</summary>
    public const int MaxRating = 5;
}
