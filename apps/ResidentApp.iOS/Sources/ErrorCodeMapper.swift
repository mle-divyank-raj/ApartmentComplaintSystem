import Foundation

// MARK: - ErrorCodeMapper
// Maps API errorCode strings (from error_codes.md) to localised user-facing messages.

enum ErrorCodeMapper {
    static func message(for error: APIError) -> String {
        switch error {
        case .unauthorized:
            return localised("error_auth_token_expired")
        case .httpError(_, let problemDetails):
            guard let code = problemDetails?.errorCode else {
                return localised("error_generic")
            }
            return message(forCode: code, validationErrors: problemDetails?.errors)
        case .networkError:
            return localised("error_network")
        case .decodingError:
            return localised("error_generic")
        case .missingBaseURL:
            return localised("error_generic")
        }
    }

    private static func message(forCode code: String, validationErrors: [String: [String]]?) -> String {
        switch code {
        // Auth
        case "Auth.InvalidCredentials":
            return localised("error_auth_invalid_credentials")
        case "Auth.AccountDeactivated":
            return localised("error_auth_account_deactivated")
        case "Auth.InvitationTokenInvalid":
            return localised("error_auth_invitation_invalid")
        case "Auth.InvitationTokenExpired":
            return localised("error_auth_invitation_expired")
        case "Auth.InvitationTokenAlreadyUsed":
            return localised("error_auth_invitation_used")
        case "Auth.InvitationTokenRevoked":
            return localised("error_auth_invitation_revoked")
        case "Auth.EmailAlreadyRegistered":
            return localised("error_auth_email_already_registered")
        // Validation
        case "Validation.Failed":
            if let errors = validationErrors, !errors.isEmpty {
                let messages = errors.values.flatMap { $0 }.joined(separator: "\n")
                return messages
            }
            return localised("error_validation_failed")
        // Complaints
        case "Complaint.NotFound":
            return localised("error_complaint_not_found")
        case "Complaint.MaxMediaAttachmentsExceeded":
            return localised("error_complaint_max_media")
        case "Complaint.InvalidMediaType":
            return localised("error_complaint_invalid_media_type")
        case "Complaint.MediaFileTooLarge":
            return localised("error_complaint_media_too_large")
        case "Complaint.InvalidStatusTransition":
            return localised("error_complaint_invalid_status_transition")
        case "Complaint.FeedbackAlreadySubmitted":
            return localised("error_complaint_feedback_already_submitted")
        case "Complaint.FeedbackNotAllowed":
            return localised("error_complaint_feedback_not_allowed")
        case "Complaint.AlreadyResolved":
            return localised("error_complaint_already_resolved")
        // System
        case "System.InternalError":
            return localised("error_generic")
        case "System.StorageUnavailable":
            return localised("error_system_storage_unavailable")
        case "System.DatabaseUnavailable":
            return localised("error_generic")
        default:
            return localised("error_generic")
        }
    }

    private static func localised(_ key: String) -> String {
        NSLocalizedString(key, comment: "")
    }
}
