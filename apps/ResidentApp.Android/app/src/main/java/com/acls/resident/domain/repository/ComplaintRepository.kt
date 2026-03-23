package com.acls.resident.domain.repository

import android.net.Uri
import com.acls.resident.domain.model.Complaint
import com.acls.resident.domain.model.ComplaintsPage

interface ComplaintRepository {
    suspend fun getMyComplaints(page: Int = 1, pageSize: Int = 20): ComplaintsPage
    suspend fun getComplaint(complaintId: Int): Complaint
    suspend fun submitComplaint(
        title: String,
        description: String,
        category: String,
        urgency: String,
        permissionToEnter: Boolean,
        mediaUris: List<Uri>
    ): Complaint
    suspend fun triggerSos(
        title: String,
        description: String,
        permissionToEnter: Boolean
    ): Complaint
    suspend fun submitFeedback(complaintId: Int, rating: Int, comment: String?): Complaint
}
