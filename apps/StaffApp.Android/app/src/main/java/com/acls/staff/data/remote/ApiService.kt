package com.acls.staff.data.remote

import com.acls.staff.data.remote.dto.AddWorkNoteRequestDto
import com.acls.staff.data.remote.dto.AuthTokenResponseDto
import com.acls.staff.data.remote.dto.ComplaintDto
import com.acls.staff.data.remote.dto.LoginRequestDto
import com.acls.staff.data.remote.dto.StaffMemberDto
import com.acls.staff.data.remote.dto.StaffMemberWithAssignmentsDto
import com.acls.staff.data.remote.dto.UpdateAvailabilityRequestDto
import com.acls.staff.data.remote.dto.UpdateComplaintStatusRequestDto
import com.acls.staff.data.remote.dto.UpdateEtaRequestDto
import com.acls.staff.data.remote.dto.WorkNoteDto
import okhttp3.MultipartBody
import okhttp3.RequestBody
import retrofit2.http.Body
import retrofit2.http.GET
import retrofit2.http.Multipart
import retrofit2.http.POST
import retrofit2.http.Part
import retrofit2.http.Path

interface ApiService {

    @POST("auth/login")
    suspend fun login(@Body request: LoginRequestDto): AuthTokenResponseDto

    @GET("staff/me")
    suspend fun getMyProfile(): StaffMemberWithAssignmentsDto

    @GET("complaints/{complaintId}")
    suspend fun getComplaint(@Path("complaintId") complaintId: Int): ComplaintDto

    @POST("complaints/{complaintId}/status")
    suspend fun updateComplaintStatus(
        @Path("complaintId") complaintId: Int,
        @Body request: UpdateComplaintStatusRequestDto
    ): ComplaintDto

    @POST("complaints/{complaintId}/eta")
    suspend fun updateEta(
        @Path("complaintId") complaintId: Int,
        @Body request: UpdateEtaRequestDto
    ): ComplaintDto

    @POST("complaints/{complaintId}/work-notes")
    suspend fun addWorkNote(
        @Path("complaintId") complaintId: Int,
        @Body request: AddWorkNoteRequestDto
    ): WorkNoteDto

    @Multipart
    @POST("complaints/{complaintId}/resolve")
    suspend fun resolveComplaint(
        @Path("complaintId") complaintId: Int,
        @Part("resolutionNotes") resolutionNotes: RequestBody,
        @Part completionPhotos: List<MultipartBody.Part>
    ): ComplaintDto

    @POST("staff/{staffMemberId}/availability")
    suspend fun updateAvailability(
        @Path("staffMemberId") staffMemberId: Int,
        @Body request: UpdateAvailabilityRequestDto
    ): StaffMemberDto
}
