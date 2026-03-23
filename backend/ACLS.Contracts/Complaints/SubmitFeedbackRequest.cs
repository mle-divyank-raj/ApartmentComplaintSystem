namespace ACLS.Contracts.Complaints;

public sealed class SubmitFeedbackRequest
{
    public int Rating { get; init; }
    public string? Comment { get; init; }
}
