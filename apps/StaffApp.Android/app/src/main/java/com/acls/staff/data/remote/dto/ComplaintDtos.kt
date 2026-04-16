package com.acls.staff.data.remote.dto

import com.google.gson.annotations.SerializedName

data class MediaDto(
    @SerializedName("mediaId") val mediaId: Int,
    @SerializedName("url") val url: String,
    @SerializedName("type") val type: String,
    @SerializedName("uploadedAt") val uploadedAt: String
)

data class WorkNoteDto(
    @SerializedName("workNoteId") val workNoteId: Int,
    @SerializedName("content") val content: String,
    @SerializedName("staffMemberId") val staffMemberId: Int,
    @SerializedName("staffMemberName") val staffMemberName: String?,
    @SerializedName("createdAt") val createdAt: String
)

data class StaffMemberSummaryDto(
    @SerializedName("staffMemberId") val staffMemberId: Int,
    @SerializedName("fullName") val fullName: String,
    @SerializedName("jobTitle") val jobTitle: String?,
    @SerializedName("availability") val availability: String?
)

data class ComplaintDto(
    @SerializedName("complaintId") val complaintId: Int,
    @SerializedName("propertyId") val propertyId: Int,
    @SerializedName("unitId") val unitId: Int,
    @SerializedName("unitNumber") val unitNumber: String?,
    @SerializedName("buildingName") val buildingName: String?,
    @SerializedName("residentId") val residentId: Int,
    @SerializedName("residentName") val residentName: String?,
    @SerializedName("title") val title: String,
    @SerializedName("description") val description: String,
    @SerializedName("category") val category: String,
    @SerializedName("urgency") val urgency: String,
    @SerializedName("status") val status: String,
    @SerializedName("permissionToEnter") val permissionToEnter: Boolean,
    @SerializedName("assignedStaffMember") val assignedStaffMember: StaffMemberSummaryDto?,
    @SerializedName("media") val media: List<MediaDto>?,
    @SerializedName("workNotes") val workNotes: List<WorkNoteDto>?,
    @SerializedName("eta") val eta: String?,
    @SerializedName("createdAt") val createdAt: String,
    @SerializedName("updatedAt") val updatedAt: String,
    @SerializedName("resolvedAt") val resolvedAt: String?,
    @SerializedName("tat") val tat: Double?,
    @SerializedName("residentRating") val residentRating: Int?,
    @SerializedName("residentFeedbackComment") val residentFeedbackComment: String?
)

data class ComplaintsPageDto(
    @SerializedName("items") val items: List<ComplaintDto>,
    @SerializedName("totalCount") val totalCount: Int,
    @SerializedName("page") val page: Int,
    @SerializedName("pageSize") val pageSize: Int
)

data class AddWorkNoteRequestDto(
    @SerializedName("content") val content: String
)

data class UpdateComplaintStatusRequestDto(
    @SerializedName("status") val status: String
)

data class UpdateEtaRequestDto(
    @SerializedName("eta") val eta: String
)
