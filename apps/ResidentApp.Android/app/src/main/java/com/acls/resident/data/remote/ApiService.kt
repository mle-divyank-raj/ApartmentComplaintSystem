package com.acls.resident.data.remote

import com.acls.resident.data.remote.dto.AuthTokenResponseDto
import com.acls.resident.data.remote.dto.ComplaintDto
import com.acls.resident.data.remote.dto.ComplaintsPageDto
import com.acls.resident.data.remote.dto.LoginRequestDto
import com.acls.resident.data.remote.dto.OutageDto
import com.acls.resident.data.remote.dto.RegisterResidentRequestDto
import com.acls.resident.data.remote.dto.SubmitFeedbackRequestDto
import com.acls.resident.data.remote.dto.TriggerSosRequestDto
import okhttp3.MultipartBody
import okhttp3.RequestBody
import retrofit2.http.Body
import retrofit2.http.GET
import retrofit2.http.Multipart
import retrofit2.http.POST
import retrofit2.http.Part
import retrofit2.http.Path
import retrofit2.http.Query

interface ApiService {

    // ── Auth ────────────────────────────────────────────────────────────────

    @POST("auth/register")
    suspend fun register(@Body request: RegisterResidentRequestDto): AuthTokenResponseDto

    @POST("auth/login")
    suspend fun login(@Body request: LoginRequestDto): AuthTokenResponseDto

    // ── Complaints ──────────────────────────────────────────────────────────

    @Multipart
    @POST("complaints")
    suspend fun submitComplaint(
        @Part("title") title: RequestBody,
        @Part("description") description: RequestBody,
        @Part("category") category: RequestBody,
        @Part("urgency") urgency: RequestBody,
        @Part("permissionToEnter") permissionToEnter: RequestBody,
        @Part mediaFiles: List<MultipartBody.Part>
    ): ComplaintDto

    @GET("complaints/my")
    suspend fun getMyComplaints(
        @Query("page") page: Int = 1,
        @Query("pageSize") pageSize: Int = 20
    ): ComplaintsPageDto

    @POST("complaints/sos")
    suspend fun triggerSos(@Body request: TriggerSosRequestDto): ComplaintDto

    @GET("complaints/{complaintId}")
    suspend fun getComplaint(@Path("complaintId") complaintId: Int): ComplaintDto

    @POST("complaints/{complaintId}/feedback")
    suspend fun submitFeedback(
        @Path("complaintId") complaintId: Int,
        @Body request: SubmitFeedbackRequestDto
    ): ComplaintDto

    // ── Outages ─────────────────────────────────────────────────────────────

    @GET("outages")
    suspend fun getOutages(): List<OutageDto>
}
