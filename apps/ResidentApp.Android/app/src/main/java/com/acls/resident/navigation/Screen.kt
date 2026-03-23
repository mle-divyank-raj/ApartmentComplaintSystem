package com.acls.resident.navigation

sealed class Screen(val route: String) {
    data object Login : Screen("login")
    data object Register : Screen("register")
    data object Home : Screen("home")
    data object ComplaintHistory : Screen("complaints/history")
    data object SubmitComplaint : Screen("complaints/submit")
    data object Sos : Screen("complaints/sos")
    data object Outages : Screen("outages")

    data object ComplaintDetail : Screen("complaints/detail/{complaintId}") {
        fun createRoute(complaintId: Int) = "complaints/detail/$complaintId"
    }

    data object Feedback : Screen("complaints/feedback/{complaintId}") {
        fun createRoute(complaintId: Int) = "complaints/feedback/$complaintId"
    }
}
