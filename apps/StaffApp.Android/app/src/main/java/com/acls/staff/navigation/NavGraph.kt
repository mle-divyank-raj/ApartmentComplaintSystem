package com.acls.staff.navigation

import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.navigation.NavType
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import androidx.navigation.compose.rememberNavController
import androidx.navigation.navArgument
import com.acls.staff.session.SessionManager
import com.acls.staff.ui.screens.availability.UpdateAvailabilityScreen
import com.acls.staff.ui.screens.complaintdetail.ComplaintDetailScreen
import com.acls.staff.ui.screens.login.LoginScreen
import com.acls.staff.ui.screens.mytasks.MyTasksScreen
import com.acls.staff.ui.screens.resolve.ResolveComplaintScreen
import com.acls.staff.ui.screens.updateeta.UpdateEtaScreen
import com.acls.staff.ui.screens.updatestatus.UpdateStatusScreen
import com.acls.staff.ui.screens.worknote.AddWorkNoteScreen

@Composable
fun StaffNavGraph(sessionManager: SessionManager) {
    val navController = rememberNavController()
    val isLoggedIn by sessionManager.isLoggedIn().collectAsStateWithLifecycle(initialValue = false)

    val startDestination = if (isLoggedIn) Screen.MyTasks.route else Screen.Login.route

    NavHost(navController = navController, startDestination = startDestination) {
        composable(Screen.Login.route) {
            LoginScreen(
                onLoginSuccess = {
                    navController.navigate(Screen.MyTasks.route) {
                        popUpTo(Screen.Login.route) { inclusive = true }
                    }
                }
            )
        }

        composable(Screen.MyTasks.route) {
            MyTasksScreen(
                onComplaintClick = { id ->
                    navController.navigate(Screen.ComplaintDetail().createRoute(id))
                },
                onUpdateAvailabilityClick = {
                    navController.navigate(Screen.UpdateAvailability.route)
                },
                onSignOut = {
                    navController.navigate(Screen.Login.route) {
                        popUpTo(0) { inclusive = true }
                    }
                }
            )
        }

        composable(
            route = Screen.ComplaintDetail().route,
            arguments = listOf(navArgument("complaintId") { type = NavType.IntType })
        ) { backStackEntry ->
            val complaintId = backStackEntry.arguments?.getInt("complaintId") ?: return@composable
            ComplaintDetailScreen(
                complaintId = complaintId,
                onUpdateStatusClick = { currentStatus ->
                    navController.navigate(Screen.UpdateStatus().createRoute(complaintId, currentStatus))
                },
                onUpdateEtaClick = { navController.navigate(Screen.UpdateEta().createRoute(complaintId)) },
                onAddWorkNoteClick = { navController.navigate(Screen.AddWorkNote().createRoute(complaintId)) },
                onResolveClick = { navController.navigate(Screen.ResolveComplaint().createRoute(complaintId)) },
                onNavigateUp = { navController.navigateUp() }
            )
        }

        composable(
            route = Screen.UpdateStatus().route,
            arguments = listOf(
                navArgument("complaintId") { type = NavType.IntType },
                navArgument("currentStatus") { type = NavType.StringType; defaultValue = "" }
            )
        ) { backStackEntry ->
            val complaintId = backStackEntry.arguments?.getInt("complaintId") ?: return@composable
            UpdateStatusScreen(
                complaintId = complaintId,
                onSuccess = { navController.navigateUp() },
                onNavigateUp = { navController.navigateUp() }
            )
        }

        composable(
            route = Screen.UpdateEta().route,
            arguments = listOf(navArgument("complaintId") { type = NavType.IntType })
        ) { backStackEntry ->
            val complaintId = backStackEntry.arguments?.getInt("complaintId") ?: return@composable
            UpdateEtaScreen(
                complaintId = complaintId,
                onSuccess = { navController.navigateUp() },
                onNavigateUp = { navController.navigateUp() }
            )
        }

        composable(
            route = Screen.AddWorkNote().route,
            arguments = listOf(navArgument("complaintId") { type = NavType.IntType })
        ) { backStackEntry ->
            val complaintId = backStackEntry.arguments?.getInt("complaintId") ?: return@composable
            AddWorkNoteScreen(
                complaintId = complaintId,
                onSuccess = { navController.navigateUp() },
                onNavigateUp = { navController.navigateUp() }
            )
        }

        composable(
            route = Screen.ResolveComplaint().route,
            arguments = listOf(navArgument("complaintId") { type = NavType.IntType })
        ) { backStackEntry ->
            val complaintId = backStackEntry.arguments?.getInt("complaintId") ?: return@composable
            ResolveComplaintScreen(
                complaintId = complaintId,
                onSuccess = {
                    navController.navigate(Screen.MyTasks.route) {
                        popUpTo(Screen.MyTasks.route) { inclusive = true }
                    }
                },
                onNavigateUp = { navController.navigateUp() }
            )
        }

        composable(Screen.UpdateAvailability.route) {
            UpdateAvailabilityScreen(
                onSuccess = { navController.navigateUp() },
                onNavigateUp = { navController.navigateUp() }
            )
        }
    }
}
