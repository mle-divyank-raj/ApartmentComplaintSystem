package com.acls.resident.navigation

import androidx.compose.runtime.Composable
import androidx.navigation.NavType
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import androidx.navigation.compose.rememberNavController
import androidx.navigation.navArgument
import com.acls.resident.session.SessionManager
import com.acls.resident.ui.screens.complaints.detail.ComplaintDetailScreen
import com.acls.resident.ui.screens.complaints.feedback.FeedbackScreen
import com.acls.resident.ui.screens.complaints.history.ComplaintHistoryScreen
import com.acls.resident.ui.screens.complaints.sos.SosScreen
import com.acls.resident.ui.screens.complaints.submit.SubmitComplaintScreen
import com.acls.resident.ui.screens.home.HomeScreen
import com.acls.resident.ui.screens.login.LoginScreen
import com.acls.resident.ui.screens.outages.OutagesScreen
import com.acls.resident.ui.screens.register.RegisterScreen

@Composable
fun ResidentNavGraph(sessionManager: SessionManager) {
    val navController = rememberNavController()
    val startDestination = if (sessionManager.isLoggedIn()) Screen.Home.route else Screen.Login.route

    NavHost(navController = navController, startDestination = startDestination) {

        composable(Screen.Login.route) {
            LoginScreen(
                onLoginSuccess = {
                    navController.navigate(Screen.Home.route) {
                        popUpTo(Screen.Login.route) { inclusive = true }
                    }
                },
                onNavigateToRegister = { navController.navigate(Screen.Register.route) }
            )
        }

        composable(Screen.Register.route) {
            RegisterScreen(
                onRegisterSuccess = {
                    navController.navigate(Screen.Home.route) {
                        popUpTo(Screen.Register.route) { inclusive = true }
                    }
                },
                onNavigateBack = { navController.popBackStack() }
            )
        }

        composable(Screen.Home.route) {
            HomeScreen(
                onNavigateToHistory = { navController.navigate(Screen.ComplaintHistory.route) },
                onNavigateToSubmit = { navController.navigate(Screen.SubmitComplaint.route) },
                onNavigateToSos = { navController.navigate(Screen.Sos.route) },
                onNavigateToOutages = { navController.navigate(Screen.Outages.route) },
                onSignOut = {
                    sessionManager.clearSession()
                    navController.navigate(Screen.Login.route) {
                        popUpTo(0) { inclusive = true }
                    }
                }
            )
        }

        composable(Screen.ComplaintHistory.route) {
            ComplaintHistoryScreen(
                onComplaintClick = { id ->
                    navController.navigate(Screen.ComplaintDetail.createRoute(id))
                },
                onNavigateBack = { navController.popBackStack() }
            )
        }

        composable(Screen.SubmitComplaint.route) {
            SubmitComplaintScreen(
                onSubmitSuccess = { navController.popBackStack() },
                onNavigateBack = { navController.popBackStack() }
            )
        }

        composable(Screen.Sos.route) {
            SosScreen(
                onSosSubmitted = { navController.popBackStack() },
                onNavigateBack = { navController.popBackStack() }
            )
        }

        composable(
            route = Screen.ComplaintDetail.route,
            arguments = listOf(navArgument("complaintId") { type = NavType.IntType })
        ) { backStackEntry ->
            val complaintId = backStackEntry.arguments?.getInt("complaintId") ?: return@composable
            ComplaintDetailScreen(
                complaintId = complaintId,
                onNavigateToFeedback = { id -> navController.navigate(Screen.Feedback.createRoute(id)) },
                onNavigateBack = { navController.popBackStack() }
            )
        }

        composable(
            route = Screen.Feedback.route,
            arguments = listOf(navArgument("complaintId") { type = NavType.IntType })
        ) { backStackEntry ->
            val complaintId = backStackEntry.arguments?.getInt("complaintId") ?: return@composable
            FeedbackScreen(
                complaintId = complaintId,
                onFeedbackSubmitted = {
                    navController.popBackStack(Screen.ComplaintHistory.route, inclusive = false)
                },
                onNavigateBack = { navController.popBackStack() }
            )
        }

        composable(Screen.Outages.route) {
            OutagesScreen(onNavigateBack = { navController.popBackStack() })
        }
    }
}
