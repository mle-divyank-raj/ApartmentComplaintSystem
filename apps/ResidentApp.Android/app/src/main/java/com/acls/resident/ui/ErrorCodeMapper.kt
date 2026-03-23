package com.acls.resident.ui

import android.content.Context
import com.acls.resident.R
import com.acls.resident.data.remote.ApiException

/**
 * Maps API error codes to user-facing string resources.
 * Falls back to the generic error message if the code is unknown.
 */
object ErrorCodeMapper {

    private val mapping = mapOf(
        "Auth.InvalidCredentials" to R.string.error_auth_invalid_credentials,
        "Auth.AccountDeactivated" to R.string.error_auth_account_deactivated,
        "Auth.InvitationTokenExpired" to R.string.error_auth_invitation_expired,
        "Auth.InvitationTokenInvalid" to R.string.error_auth_invitation_invalid,
        "Auth.InvitationTokenAlreadyUsed" to R.string.error_auth_invitation_already_used,
        "Auth.InvitationTokenRevoked" to R.string.error_auth_invitation_revoked,
        "Auth.EmailAlreadyRegistered" to R.string.error_auth_email_already_registered,
        "Validation.Failed" to R.string.error_validation_failed,
        "Complaint.MaxMediaAttachmentsExceeded" to R.string.error_complaint_max_media,
        "Complaint.InvalidMediaType" to R.string.error_complaint_invalid_media_type,
        "Complaint.MediaFileTooLarge" to R.string.error_complaint_media_too_large,
        "Complaint.FeedbackAlreadySubmitted" to R.string.error_complaint_feedback_already_submitted,
        "Complaint.FeedbackNotAllowed" to R.string.error_complaint_feedback_not_allowed,
        "Complaint.NotFound" to R.string.error_complaint_not_found,
        "System.InternalError" to R.string.error_generic,
        "System.StorageUnavailable" to R.string.error_generic,
        "System.DatabaseUnavailable" to R.string.error_generic
    )

    fun getMessage(context: Context, exception: Throwable): String {
        if (exception is ApiException) {
            val resId = mapping[exception.errorCode]
            if (resId != null) return context.getString(resId)
        }
        return context.getString(R.string.error_generic)
    }

    fun getMessage(context: Context, errorCode: String): String {
        val resId = mapping[errorCode]
        return if (resId != null) context.getString(resId) else context.getString(R.string.error_generic)
    }
}
