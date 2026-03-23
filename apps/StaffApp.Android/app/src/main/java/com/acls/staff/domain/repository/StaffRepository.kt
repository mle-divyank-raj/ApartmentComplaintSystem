package com.acls.staff.domain.repository

import com.acls.staff.domain.model.Staff
import com.acls.staff.domain.model.StaffMemberSummary

interface StaffRepository {
    suspend fun getMyProfile(): Staff
    suspend fun updateAvailability(staffMemberId: Int, availability: String): StaffMemberSummary
}
