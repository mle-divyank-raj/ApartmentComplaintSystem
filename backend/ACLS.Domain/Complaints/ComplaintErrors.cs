using ACLS.SharedKernel;

namespace ACLS.Domain.Complaints;

/// <summary>
/// Domain-level error definitions for the Complaints bounded context.
/// All errors follow the "Complaint.[ErrorName]" code convention.
/// These are the only permitted sources of Complaint-related Error instances.
/// </summary>
public static class ComplaintErrors
{
    public static Error NotFound(int complaintId) =>
        new("Complaint.NotFound", $"Complaint with ID {complaintId} was not found.");

    public static Error AccessDenied() =>
        new("Complaint.AccessDenied", "You do not have access to this complaint.");

    public static Error InvalidStatusTransition(TicketStatus from, TicketStatus to) =>
        new("Complaint.InvalidStatusTransition",
            $"Cannot transition a complaint from {from} to {to}.");

    public static Error MaxMediaAttachmentsExceeded() =>
        new("Complaint.MaxMediaAttachmentsExceeded",
            $"A complaint may not have more than {ComplaintConstants.MaxMediaAttachments} " +
            "resident-uploaded media attachments.");

    public static Error InvalidRating(int rating) =>
        new("Complaint.InvalidRating",
            $"Rating {rating} is not valid. Rating must be between " +
            $"{ComplaintConstants.MinRating} and {ComplaintConstants.MaxRating}.");

    public static Error CannotUpdateEtaOnClosedComplaint() =>
        new("Complaint.CannotUpdateEta",
            "ETA cannot be updated on a RESOLVED or CLOSED complaint.");

    public static Error EtaMustBeInFuture() =>
        new("Complaint.EtaMustBeInFuture",
            "ETA must be a future UTC date and time.");

    public static Error AlreadyAssigned() =>
        new("Complaint.AlreadyAssigned",
            "This complaint is already assigned to a staff member.");

    public static Error StaffNotAvailable(int staffMemberId) =>
        new("Complaint.StaffNotAvailable",
            $"Staff member {staffMemberId} is not AVAILABLE and cannot be assigned.");

    public static Error MediaFileTooLarge(string fileName) =>
        new("Complaint.MediaFileTooLarge",
            $"File '{fileName}' exceeds the 5 MB size limit.");

    public static Error InvalidMediaType(string contentType) =>
        new("Complaint.InvalidMediaType",
            $"File type '{contentType}' is not accepted — only JPEG and PNG are allowed.");

    public static Error AlreadyClosed(int complaintId) =>
        new("Complaint.AlreadyClosed",
            $"Complaint {complaintId} is CLOSED and cannot be modified.");

    public static Error AlreadyResolved(int complaintId) =>
        new("Complaint.AlreadyResolved",
            $"Complaint {complaintId} is already RESOLVED.");

    public static Error NotAssigned(int complaintId) =>
        new("Complaint.NotAssigned",
            $"Complaint {complaintId} is not ASSIGNED. This operation requires the complaint to be assigned first.");

    public static Error StaffBelongsToDifferentProperty(int staffMemberId) =>
        new("Complaint.StaffBelongsToDifferentProperty",
            $"Staff member {staffMemberId} does not belong to this property.");

    public static Error FeedbackAlreadySubmitted(int complaintId) =>
        new("Complaint.FeedbackAlreadySubmitted",
            $"Resident has already submitted feedback for complaint {complaintId}.");

    public static Error FeedbackNotAllowed(TicketStatus currentStatus) =>
        new("Complaint.FeedbackNotAllowed",
            $"Feedback can only be submitted when the complaint is RESOLVED. Current status: {currentStatus}.");
}
