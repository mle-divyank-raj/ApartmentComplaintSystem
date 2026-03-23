package com.acls.staff.data.remote

import com.google.gson.Gson
import com.google.gson.annotations.SerializedName
import retrofit2.HttpException

/**
 * RFC 7807 Problem Details response body.
 */
data class ProblemDetails(
    @SerializedName("type") val type: String? = null,
    @SerializedName("title") val title: String? = null,
    @SerializedName("status") val status: Int? = null,
    @SerializedName("detail") val detail: String? = null,
    @SerializedName("errorCode") val errorCode: String? = null,
    @SerializedName("errors") val errors: Map<String, List<String>>? = null
)

/**
 * Exception carrying a parsed errorCode from the API.
 */
class ApiException(
    val errorCode: String,
    val detail: String?,
    val httpStatus: Int
) : Exception(detail ?: errorCode)

/**
 * Parses a Retrofit [HttpException] into an [ApiException] with the errorCode extracted
 * from the RFC 7807 response body. Falls back to a generic error code if parsing fails.
 */
fun HttpException.toApiException(gson: Gson): ApiException {
    val body = response()?.errorBody()?.string()
    if (body != null) {
        try {
            val problem = gson.fromJson(body, ProblemDetails::class.java)
            if (problem?.errorCode != null) {
                return ApiException(
                    errorCode = problem.errorCode,
                    detail = problem.detail,
                    httpStatus = code()
                )
            }
        } catch (_: Exception) { /* fall through */ }
    }
    return ApiException(
        errorCode = "System.InternalError",
        detail = message(),
        httpStatus = code()
    )
}
