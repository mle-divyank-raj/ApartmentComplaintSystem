package com.acls.staff.domain.model

data class Media(
    val mediaId: Int,
    val url: String,
    val type: String,
    val uploadedAt: String
)

data class WorkNote(
    val workNoteId: Int,
    val content: String,
    val staffMemberId: Int,
    val staffMemberName: String,
    val createdAt: String
)

data class StaffMemberSummary(
    val staffMemberId: Int,
    val fullName: String,
    val jobTitle: String?,
    val availability: String
)

data class Complaint(
    val complaintId: Int,
    val propertyId: Int,
    val unitId: Int,
    val unitNumber: String?,
    val buildingName: String?,
    val residentId: Int,
    val residentName: String?,
    val title: String,
    val description: String,
    val category: String,
    val urgency: String,
    val status: String,
    val permissionToEnter: Boolean,
    val assignedStaffMember: StaffMemberSummary?,
    val media: List<Media>,
    val workNotes: List<WorkNote>,
    val eta: String?,
    val createdAt: String,
    val updatedAt: String,
    val resolvedAt: String?,
    val tat: Double?,
    val residentRating: Int?,
    val residentFeedbackComment: String?
)
