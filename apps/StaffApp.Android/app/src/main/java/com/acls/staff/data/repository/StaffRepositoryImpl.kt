package com.acls.staff.data.repository

import com.acls.staff.data.remote.ApiService
import com.acls.staff.data.remote.dto.StaffMemberWithAssignmentsDto
import com.acls.staff.data.remote.dto.UpdateAvailabilityRequestDto
import com.acls.staff.domain.model.Complaint
import com.acls.staff.domain.model.Media
import com.acls.staff.domain.model.Staff
import com.acls.staff.domain.model.StaffMemberSummary
import com.acls.staff.domain.model.WorkNote
import com.acls.staff.domain.repository.StaffRepository
import com.acls.staff.session.SessionManager
import javax.inject.Inject

class StaffRepositoryImpl @Inject constructor(
    private val apiService: ApiService,
    private val sessionManager: SessionManager
) : StaffRepository {

    override suspend fun getMyProfile(): Staff {
        val dto = apiService.getMyProfile()
        val staff = dto.toDomain()
        sessionManager.saveStaffMemberId(staff.staffMemberId)
        return staff
    }

    override suspend fun updateAvailability(staffMemberId: Int, availability: String): StaffMemberSummary {
        val dto = apiService.updateAvailability(
            staffMemberId,
            UpdateAvailabilityRequestDto(availability = availability)
        )
        return StaffMemberSummary(
            staffMemberId = dto.staffMemberId,
            fullName = dto.fullName,
            jobTitle = dto.jobTitle,
            availability = dto.availability
        )
    }

    private fun StaffMemberWithAssignmentsDto.toDomain() = Staff(
        staffMemberId = profile.staffMemberId,
        userId = profile.userId,
        fullName = profile.fullName,
        jobTitle = profile.jobTitle,
        skills = profile.skills,
        availability = profile.availability,
        averageRating = profile.averageRating,
        lastAssignedAt = profile.lastAssignedAt,
        activeAssignments = activeAssignments.map { c ->
            Complaint(
                complaintId = c.complaintId,
                propertyId = c.propertyId,
                unitId = c.unitId,
                unitNumber = c.unitNumber,
                buildingName = c.buildingName,
                residentId = c.residentId,
                residentName = c.residentName,
                title = c.title,
                description = c.description,
                category = c.category,
                urgency = c.urgency,
                status = c.status,
                permissionToEnter = c.permissionToEnter,
                assignedStaffMember = c.assignedStaffMember?.let {
                    StaffMemberSummary(it.staffMemberId, it.fullName, it.jobTitle, it.availability ?: "")
                },
                media = c.media?.map { Media(it.mediaId, it.url, it.type, it.uploadedAt) } ?: emptyList(),
                workNotes = c.workNotes?.map {
                    WorkNote(it.workNoteId, it.content, it.staffMemberId, it.staffMemberName ?: "", it.createdAt)
                } ?: emptyList(),
                eta = c.eta,
                createdAt = c.createdAt,
                updatedAt = c.updatedAt,
                resolvedAt = c.resolvedAt,
                tat = c.tat,
                residentRating = c.residentRating,
                residentFeedbackComment = c.residentFeedbackComment
            )
        }
    )
}
