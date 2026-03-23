package com.acls.resident.domain.model

data class Complaint(
    val complaintId: Int,
    val title: String,
    val description: String,
    val category: String,
    val urgency: String,
    val status: String,
    val unitNumber: String?,
    val buildingName: String?,
    val permissionToEnter: Boolean,
    val assignedStaffName: String?,
    val mediaUrls: List<String>,
    val workNotes: List<WorkNote>,
    val eta: String?,
    val createdAt: String,
    val updatedAt: String,
    val resolvedAt: String?,
    val tat: Double?,
    val residentRating: Int?,
    val residentFeedbackComment: String?
)

data class WorkNote(
    val workNoteId: Int,
    val content: String,
    val staffMemberName: String,
    val createdAt: String
)

data class ComplaintsPage(
    val items: List<Complaint>,
    val totalCount: Int,
    val page: Int,
    val pageSize: Int
)
