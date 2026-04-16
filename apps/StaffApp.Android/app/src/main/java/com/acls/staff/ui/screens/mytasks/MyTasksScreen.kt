package com.acls.staff.ui.screens.mytasks

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Refresh
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.material3.TopAppBar
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import androidx.hilt.navigation.compose.hiltViewModel
import androidx.lifecycle.Lifecycle
import androidx.lifecycle.compose.LocalLifecycleOwner
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.lifecycle.repeatOnLifecycle
import com.acls.staff.domain.model.Staff
import com.acls.staff.ui.components.ComplaintCard
import com.acls.staff.ui.components.ErrorScreen
import com.acls.staff.ui.components.LoadingScreen

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun MyTasksScreen(
    onComplaintClick: (Int) -> Unit,
    onUpdateAvailabilityClick: () -> Unit,
    onSignOut: () -> Unit,
    viewModel: MyTasksViewModel = hiltViewModel()
) {
    val uiState by viewModel.uiState.collectAsStateWithLifecycle()
    val lifecycleOwner = LocalLifecycleOwner.current

    LaunchedEffect(lifecycleOwner) {
        lifecycleOwner.repeatOnLifecycle(Lifecycle.State.RESUMED) {
            viewModel.loadProfile()
        }
    }

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text("My Tasks") },
                actions = {
                    IconButton(onClick = { viewModel.loadProfile() }) {
                        Icon(Icons.Default.Refresh, contentDescription = "Refresh")
                    }
                    TextButton(onClick = {
                        viewModel.signOut()
                        onSignOut()
                    }) {
                        Text("Sign Out")
                    }
                }
            )
        }
    ) { innerPadding ->
        when (val state = uiState) {
            is MyTasksUiState.Loading -> LoadingScreen(modifier = Modifier.padding(innerPadding))
            is MyTasksUiState.Error -> ErrorScreen(
                message = state.message,
                onRetry = { viewModel.loadProfile() },
                modifier = Modifier.padding(innerPadding)
            )
            is MyTasksUiState.Success -> MyTasksContent(
                staff = state.staff,
                onComplaintClick = onComplaintClick,
                onUpdateAvailabilityClick = onUpdateAvailabilityClick,
                modifier = Modifier.padding(innerPadding)
            )
        }
    }
}

@Composable
private fun MyTasksContent(
    staff: Staff,
    onComplaintClick: (Int) -> Unit,
    onUpdateAvailabilityClick: () -> Unit,
    modifier: Modifier = Modifier
) {
    LazyColumn(
        modifier = modifier
            .fillMaxSize()
            .padding(horizontal = 16.dp),
        verticalArrangement = Arrangement.spacedBy(12.dp)
    ) {
        item {
            Spacer(modifier = Modifier.height(8.dp))
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween
            ) {
                Column {
                    Text(
                        text = staff.fullName,
                        style = MaterialTheme.typography.titleLarge
                    )
                    staff.jobTitle?.let {
                        Text(
                            text = it,
                            style = MaterialTheme.typography.bodyMedium,
                            color = MaterialTheme.colorScheme.onSurfaceVariant
                        )
                    }
                }
                TextButton(onClick = onUpdateAvailabilityClick) {
                    Text(
                        text = staff.availability,
                        color = when (staff.availability.uppercase()) {
                            "AVAILABLE" -> MaterialTheme.colorScheme.primary
                            "ON_BREAK" -> MaterialTheme.colorScheme.tertiary
                            else -> MaterialTheme.colorScheme.onSurfaceVariant
                        }
                    )
                }
            }
            Spacer(modifier = Modifier.height(8.dp))
            Text(
                text = "Active Assignments (${staff.activeAssignments.size})",
                style = MaterialTheme.typography.titleLarge
            )
            Spacer(modifier = Modifier.height(4.dp))
        }

        if (staff.activeAssignments.isEmpty()) {
            item {
                Text(
                    text = "No active tasks assigned.",
                    style = MaterialTheme.typography.bodyMedium,
                    color = MaterialTheme.colorScheme.onSurfaceVariant
                )
            }
        } else {
            items(staff.activeAssignments) { complaint ->
                ComplaintCard(
                    complaint = complaint,
                    onClick = { onComplaintClick(complaint.complaintId) }
                )
            }
        }

        item { Spacer(modifier = Modifier.height(16.dp)) }
    }
}
