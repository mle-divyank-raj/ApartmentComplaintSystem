package com.acls.staff.data.remote.dto

import com.google.gson.annotations.SerializedName

data class StaffMemberDto(
    @SerializedName("staffMemberId") val staffMemberId: Int,
    @SerializedName("userId") val userId: Int,
    @SerializedName("fullName") val fullName: String,
    @SerializedName("jobTitle") val jobTitle: String?,
    @SerializedName("skills") val skills: List<String>,
    @SerializedName("availability") val availability: String,
    @SerializedName("averageRating") val averageRating: Double?,
    @SerializedName("lastAssignedAt") val lastAssignedAt: String?
)

data class StaffMemberWithAssignmentsDto(
    @SerializedName("profile") val profile: StaffMemberDto,
    @SerializedName("activeAssignments") val activeAssignments: List<ComplaintDto>
)

data class UpdateAvailabilityRequestDto(
    @SerializedName("availability") val availability: String
)
