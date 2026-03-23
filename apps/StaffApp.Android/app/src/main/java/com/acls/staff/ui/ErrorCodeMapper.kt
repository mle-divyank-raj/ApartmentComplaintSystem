package com.acls.staff.ui

import android.content.Context
import com.acls.staff.R
import com.acls.staff.data.remote.ApiException

/**
 * Maps API error codes to user-facing string resources.
 * Falls back to the generic error message if the code is unknown.
 */
object ErrorCodeMapper {

    private val mapping = mapOf(
        "Auth.InvalidCredentials" to R.string.error_auth_invalid_credentials,
        "Auth.AccountDeactivated" to R.string.error_auth_account_deactivated,
        "Validation.Failed" to R.string.error_validation_failed,
        "Complaint.InvalidStatusTransition" to R.string.error_complaint_invalid_status_transition,
        "Complaint.MaxMediaAttachmentsExceeded" to R.string.error_complaint_max_media,
        "Complaint.InvalidMediaType" to R.string.error_complaint_invalid_media_type,
        "Complaint.MediaFileTooLarge" to R.string.error_complaint_media_too_large,
        "Complaint.NotFound" to R.string.error_complaint_not_found,
        "Staff.CannotSetBusyManually" to R.string.error_staff_cannot_set_busy,
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
