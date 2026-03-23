package com.acls.staff.navigation

sealed class Screen(val route: String) {
    data object Login : Screen("login")
    data object MyTasks : Screen("my_tasks")
    data class ComplaintDetail(val complaintId: Int = 0) : Screen("complaint_detail/{complaintId}") {
        fun createRoute(id: Int) = "complaint_detail/$id"
    }
    data class UpdateStatus(val complaintId: Int = 0) : Screen("update_status/{complaintId}?currentStatus={currentStatus}") {
        fun createRoute(id: Int, currentStatus: String) = "update_status/$id?currentStatus=$currentStatus"
    }
    data class UpdateEta(val complaintId: Int = 0) : Screen("update_eta/{complaintId}") {
        fun createRoute(id: Int) = "update_eta/$id"
    }
    data class AddWorkNote(val complaintId: Int = 0) : Screen("add_work_note/{complaintId}") {
        fun createRoute(id: Int) = "add_work_note/$id"
    }
    data class ResolveComplaint(val complaintId: Int = 0) : Screen("resolve_complaint/{complaintId}") {
        fun createRoute(id: Int) = "resolve_complaint/$id"
    }
    data object UpdateAvailability : Screen("update_availability")
}
