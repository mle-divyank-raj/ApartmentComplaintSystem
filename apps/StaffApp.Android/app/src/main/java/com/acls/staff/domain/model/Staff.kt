package com.acls.staff.domain.model

data class Staff(
    val staffMemberId: Int,
    val userId: Int,
    val fullName: String,
    val jobTitle: String?,
    val skills: List<String>,
    val availability: String,
    val averageRating: Double?,
    val lastAssignedAt: String?,
    val activeAssignments: List<Complaint>
)
