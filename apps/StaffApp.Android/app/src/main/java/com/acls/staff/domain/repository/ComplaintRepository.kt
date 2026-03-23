package com.acls.staff.domain.repository

import android.net.Uri
import com.acls.staff.domain.model.Complaint
import com.acls.staff.domain.model.WorkNote

interface ComplaintRepository {
    suspend fun getComplaint(complaintId: Int): Complaint
    suspend fun updateStatus(complaintId: Int, status: String): Complaint
    suspend fun updateEta(complaintId: Int, eta: String): Complaint
    suspend fun addWorkNote(complaintId: Int, content: String): WorkNote
    suspend fun resolveComplaint(complaintId: Int, resolutionNotes: String, photos: List<Uri>): Complaint
}
