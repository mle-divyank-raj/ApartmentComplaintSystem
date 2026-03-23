package com.acls.resident.data.repository

import android.content.Context
import android.net.Uri
import com.acls.resident.data.remote.ApiService
import com.acls.resident.data.remote.dto.ComplaintDto
import com.acls.resident.data.remote.dto.SubmitFeedbackRequestDto
import com.acls.resident.data.remote.dto.TriggerSosRequestDto
import com.acls.resident.domain.model.Complaint
import com.acls.resident.domain.model.ComplaintsPage
import com.acls.resident.domain.model.WorkNote
import com.acls.resident.domain.repository.ComplaintRepository
import dagger.hilt.android.qualifiers.ApplicationContext
import okhttp3.MediaType.Companion.toMediaTypeOrNull
import okhttp3.MultipartBody
import okhttp3.RequestBody.Companion.asRequestBody
import okhttp3.RequestBody.Companion.toRequestBody
import java.io.File
import java.io.FileOutputStream
import javax.inject.Inject

class ComplaintRepositoryImpl @Inject constructor(
    private val apiService: ApiService,
    @ApplicationContext private val context: Context
) : ComplaintRepository {

    override suspend fun getMyComplaints(page: Int, pageSize: Int): ComplaintsPage {
        val dto = apiService.getMyComplaints(page, pageSize)
        return ComplaintsPage(
            items = dto.items.map { it.toDomain() },
            totalCount = dto.totalCount,
            page = dto.page,
            pageSize = dto.pageSize
        )
    }

    override suspend fun getComplaint(complaintId: Int): Complaint =
        apiService.getComplaint(complaintId).toDomain()

    override suspend fun submitComplaint(
        title: String,
        description: String,
        category: String,
        urgency: String,
        permissionToEnter: Boolean,
        mediaUris: List<Uri>
    ): Complaint {
        val textPlain = "text/plain".toMediaTypeOrNull()
        val parts = mediaUris.mapIndexed { index, uri ->
            val inputStream = context.contentResolver.openInputStream(uri)!!
            val tempFile = File.createTempFile("media_$index", null, context.cacheDir)
            FileOutputStream(tempFile).use { out -> inputStream.copyTo(out) }
            val mimeType = context.contentResolver.getType(uri) ?: "image/*"
            val requestBody = tempFile.asRequestBody(mimeType.toMediaTypeOrNull())
            MultipartBody.Part.createFormData("mediaFiles", tempFile.name, requestBody)
        }
        return apiService.submitComplaint(
            title = title.toRequestBody(textPlain),
            description = description.toRequestBody(textPlain),
            category = category.toRequestBody(textPlain),
            urgency = urgency.toRequestBody(textPlain),
            permissionToEnter = permissionToEnter.toString().toRequestBody(textPlain),
            mediaFiles = parts
        ).toDomain()
    }

    override suspend fun triggerSos(
        title: String,
        description: String,
        permissionToEnter: Boolean
    ): Complaint = apiService.triggerSos(
        TriggerSosRequestDto(
            title = title,
            description = description,
            permissionToEnter = permissionToEnter
        )
    ).toDomain()

    override suspend fun submitFeedback(complaintId: Int, rating: Int, comment: String?): Complaint =
        apiService.submitFeedback(
            complaintId = complaintId,
            request = SubmitFeedbackRequestDto(rating = rating, comment = comment)
        ).toDomain()

    private fun ComplaintDto.toDomain() = Complaint(
        complaintId = complaintId,
        title = title,
        description = description,
        category = category,
        urgency = urgency,
        status = status,
        unitNumber = unitNumber,
        buildingName = buildingName,
        permissionToEnter = permissionToEnter,
        assignedStaffName = assignedStaffMember?.fullName,
        mediaUrls = media.map { it.url },
        workNotes = workNotes.map {
            WorkNote(
                workNoteId = it.workNoteId,
                content = it.content,
                staffMemberName = it.staffMemberName,
                createdAt = it.createdAt
            )
        },
        eta = eta,
        createdAt = createdAt,
        updatedAt = updatedAt,
        resolvedAt = resolvedAt,
        tat = tat,
        residentRating = residentRating,
        residentFeedbackComment = residentFeedbackComment
    )
}
