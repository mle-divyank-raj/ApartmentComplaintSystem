import Foundation

// MARK: - ErrorCodeMapper

enum ErrorCodeMapper {
    static func message(for error: APIError) -> String {
        switch error {
        case .missingBaseURL:
            return NSLocalizedString("error_generic", comment: "")
        case .networkError:
            return NSLocalizedString("error_network", comment: "")
        case .unauthorized:
            return NSLocalizedString("error_auth_token_expired", comment: "")
        case .decodingError:
            return NSLocalizedString("error_generic", comment: "")
        case .httpError(_, let problem):
            return messageForProblem(problem)
        }
    }

    private static func messageForProblem(_ problem: ProblemDetails?) -> String {
        guard let problem else {
            return NSLocalizedString("error_generic", comment: "")
        }
        switch problem.type {
        case "Auth.InvalidCredentials":
            return NSLocalizedString("error_auth_invalid_credentials", comment: "")
        case "Auth.AccountDeactivated":
            return NSLocalizedString("error_auth_account_deactivated", comment: "")
        case "Auth.TokenExpired":
            return NSLocalizedString("error_auth_token_expired", comment: "")
        case "Auth.InvitationNotFound":
            return NSLocalizedString("error_auth_invitation_not_found", comment: "")
        case "Auth.InvitationAlreadyUsed":
            return NSLocalizedString("error_auth_invitation_already_used", comment: "")
        case "Auth.InvitationExpired":
            return NSLocalizedString("error_auth_invitation_expired", comment: "")
        case "Auth.InvitationRoleConflict":
            return NSLocalizedString("error_auth_invitation_role_conflict", comment: "")
        case "Auth.EmailAlreadyRegistered":
            return NSLocalizedString("error_auth_email_already_registered", comment: "")
        case "Validation.Failed":
            if let errors = problem.errors {
                let messages = errors.values.flatMap { $0 }
                if !messages.isEmpty {
                    return messages.joined(separator: "\n")
                }
            }
            return NSLocalizedString("error_validation_failed", comment: "")
        case "Complaint.NotFound":
            return NSLocalizedString("error_complaint_not_found", comment: "")
        case "Complaint.MaxMediaAttachmentsExceeded":
            return NSLocalizedString("error_complaint_max_media", comment: "")
        case "Complaint.InvalidMediaType":
            return NSLocalizedString("error_complaint_invalid_media_type", comment: "")
        case "Complaint.MediaTooLarge":
            return NSLocalizedString("error_complaint_media_too_large", comment: "")
        case "Complaint.InvalidStatusTransition":
            return NSLocalizedString("error_complaint_invalid_status_transition", comment: "")
        case "Complaint.AlreadyResolved":
            return NSLocalizedString("error_complaint_already_resolved", comment: "")
        case "Staff.CannotSetBusyManually":
            return NSLocalizedString("error_staff_cannot_set_busy_manually", comment: "")
        case "Staff.NotFound":
            return NSLocalizedString("error_generic", comment: "")
        case "System.StorageUnavailable":
            return NSLocalizedString("error_system_storage_unavailable", comment: "")
        default:
            return problem.detail ?? NSLocalizedString("error_generic", comment: "")
        }
    }
}
