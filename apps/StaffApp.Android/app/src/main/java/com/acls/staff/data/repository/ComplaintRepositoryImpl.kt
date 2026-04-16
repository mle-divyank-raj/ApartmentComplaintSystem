package com.acls.staff.data.repository

import android.content.Context
import android.net.Uri
import com.acls.staff.data.remote.ApiService
import com.acls.staff.data.remote.dto.AddWorkNoteRequestDto
import com.acls.staff.data.remote.dto.ComplaintDto
import com.acls.staff.data.remote.dto.UpdateComplaintStatusRequestDto
import com.acls.staff.data.remote.dto.UpdateEtaRequestDto
import com.acls.staff.data.remote.dto.WorkNoteDto
import com.acls.staff.domain.model.Complaint
import com.acls.staff.domain.model.Media
import com.acls.staff.domain.model.StaffMemberSummary
import com.acls.staff.domain.model.WorkNote
import com.acls.staff.domain.repository.ComplaintRepository
import dagger.hilt.android.qualifiers.ApplicationContext
import okhttp3.MediaType.Companion.toMediaType
import okhttp3.MultipartBody
import okhttp3.RequestBody.Companion.asRequestBody
import okhttp3.RequestBody.Companion.toRequestBody
import java.io.File
import javax.inject.Inject

class ComplaintRepositoryImpl @Inject constructor(
    private val apiService: ApiService,
    @ApplicationContext private val context: Context
) : ComplaintRepository {

    override suspend fun getComplaint(complaintId: Int): Complaint =
        apiService.getComplaint(complaintId).toDomain()

    override suspend fun updateStatus(complaintId: Int, status: String): Complaint =
        apiService.updateComplaintStatus(
            complaintId,
            UpdateComplaintStatusRequestDto(status = status)
        ).toDomain()

    override suspend fun updateEta(complaintId: Int, eta: String) {
        apiService.updateEta(complaintId, UpdateEtaRequestDto(eta = eta))
    }

    override suspend fun addWorkNote(complaintId: Int, content: String): WorkNote =
        apiService.addWorkNote(complaintId, AddWorkNoteRequestDto(content = content)).toDomain()

    override suspend fun resolveComplaint(
        complaintId: Int,
        resolutionNotes: String,
        photos: List<Uri>
    ): Complaint {
        val notesPart = resolutionNotes.toRequestBody("text/plain".toMediaType())
        val photoParts = photos.mapIndexed { index, uri ->
            val inputStream = context.contentResolver.openInputStream(uri)
                ?: throw IllegalArgumentException("Cannot open URI: $uri")
            val mimeType = context.contentResolver.getType(uri) ?: "image/jpeg"
            val bytes = inputStream.use { it.readBytes() }
            val requestBody = bytes.toRequestBody(mimeType.toMediaType())
            MultipartBody.Part.createFormData("completionPhotos", "photo_$index.jpg", requestBody)
        }
        return apiService.resolveComplaint(complaintId, notesPart, photoParts).toDomain()
    }

    private fun ComplaintDto.toDomain() = Complaint(
        complaintId = complaintId,
        propertyId = propertyId,
        unitId = unitId,
        unitNumber = unitNumber,
        buildingName = buildingName,
        residentId = residentId,
        residentName = residentName,
        title = title,
        description = description,
        category = category,
        urgency = urgency,
        status = status,
        permissionToEnter = permissionToEnter,
        assignedStaffMember = assignedStaffMember?.let {
            StaffMemberSummary(
                staffMemberId = it.staffMemberId,
                fullName = it.fullName,
                jobTitle = it.jobTitle,
                availability = it.availability ?: ""
            )
        },
        media = media?.map { Media(it.mediaId, it.url, it.type, it.uploadedAt) } ?: emptyList(),
        workNotes = workNotes?.map { it.toDomain() } ?: emptyList(),
        eta = eta,
        createdAt = createdAt,
        updatedAt = updatedAt,
        resolvedAt = resolvedAt,
        tat = tat,
        residentRating = residentRating,
        residentFeedbackComment = residentFeedbackComment
    )

    private fun WorkNoteDto.toDomain() = WorkNote(
        workNoteId = workNoteId,
        content = content,
        staffMemberId = staffMemberId,
        staffMemberName = staffMemberName ?: "",
        createdAt = createdAt
    )
}
