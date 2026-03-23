package com.acls.staff.ui.screens.complaintdetail

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material3.Button
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.material3.TopAppBar
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import androidx.hilt.navigation.compose.hiltViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import com.acls.staff.domain.model.Complaint
import com.acls.staff.ui.components.ErrorScreen
import com.acls.staff.ui.components.LoadingScreen

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun ComplaintDetailScreen(
    complaintId: Int,
    onUpdateStatusClick: (currentStatus: String) -> Unit,
    onUpdateEtaClick: () -> Unit,
    onAddWorkNoteClick: () -> Unit,
    onResolveClick: () -> Unit,
    onNavigateUp: () -> Unit,
    viewModel: ComplaintDetailViewModel = hiltViewModel()
) {
    val uiState by viewModel.uiState.collectAsStateWithLifecycle()

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text("Complaint #$complaintId") },
                navigationIcon = {
                    IconButton(onClick = onNavigateUp) {
                        Icon(Icons.AutoMirrored.Filled.ArrowBack, contentDescription = "Back")
                    }
                }
            )
        }
    ) { innerPadding ->
        when (val state = uiState) {
            is ComplaintDetailUiState.Loading -> LoadingScreen(modifier = Modifier.padding(innerPadding))
            is ComplaintDetailUiState.Error -> ErrorScreen(
                message = state.message,
                onRetry = { viewModel.loadComplaint() },
                modifier = Modifier.padding(innerPadding)
            )
            is ComplaintDetailUiState.Success -> ComplaintDetailContent(
                complaint = state.complaint,
                onUpdateStatusClick = { onUpdateStatusClick(state.complaint.status) },
                onUpdateEtaClick = onUpdateEtaClick,
                onAddWorkNoteClick = onAddWorkNoteClick,
                onResolveClick = onResolveClick,
                modifier = Modifier.padding(innerPadding)
            )
        }
    }
}

@Composable
private fun ComplaintDetailContent(
    complaint: Complaint,
    onUpdateStatusClick: () -> Unit,
    onUpdateEtaClick: () -> Unit,
    onAddWorkNoteClick: () -> Unit,
    onResolveClick: () -> Unit,
    modifier: Modifier = Modifier
) {
    Column(
        modifier = modifier
            .fillMaxSize()
            .verticalScroll(rememberScrollState())
            .padding(16.dp)
    ) {
        Text(text = complaint.title, style = MaterialTheme.typography.headlineMedium)
        Spacer(modifier = Modifier.height(4.dp))
        Text(
            text = "${complaint.status} · ${complaint.urgency} · ${complaint.category}",
            style = MaterialTheme.typography.bodyMedium,
            color = MaterialTheme.colorScheme.onSurfaceVariant
        )
        Spacer(modifier = Modifier.height(8.dp))
        complaint.unitNumber?.let {
            Text(
                text = "Unit $it${complaint.buildingName?.let { b -> " · $b" } ?: ""}",
                style = MaterialTheme.typography.bodyMedium
            )
        }
        Text(
            text = "Resident: ${complaint.residentName ?: "Unknown"}",
            style = MaterialTheme.typography.bodyMedium
        )
        Text(
            text = "Permission to enter: ${if (complaint.permissionToEnter) "Yes" else "No"}",
            style = MaterialTheme.typography.bodyMedium
        )
        complaint.eta?.let {
            Text(text = "ETA: ${it.take(16).replace("T", " ")}", style = MaterialTheme.typography.bodyMedium)
        }

        Spacer(modifier = Modifier.height(12.dp))
        Text(text = "Description", style = MaterialTheme.typography.titleLarge)
        Text(text = complaint.description, style = MaterialTheme.typography.bodyLarge)

        if (complaint.workNotes.isNotEmpty()) {
            Spacer(modifier = Modifier.height(12.dp))
            HorizontalDivider()
            Spacer(modifier = Modifier.height(8.dp))
            Text(text = "Work Notes", style = MaterialTheme.typography.titleLarge)
            complaint.workNotes.forEach { note ->
                Spacer(modifier = Modifier.height(4.dp))
                Text(
                    text = "• ${note.content}",
                    style = MaterialTheme.typography.bodyMedium
                )
                Text(
                    text = "  — ${note.staffMemberName}, ${note.createdAt.take(10)}",
                    style = MaterialTheme.typography.bodyMedium,
                    color = MaterialTheme.colorScheme.onSurfaceVariant
                )
            }
        }

        Spacer(modifier = Modifier.height(24.dp))
        HorizontalDivider()
        Spacer(modifier = Modifier.height(12.dp))
        Text(text = "Actions", style = MaterialTheme.typography.titleLarge)
        Spacer(modifier = Modifier.height(8.dp))

        val isResolved = complaint.status.uppercase() == "RESOLVED"

        if (!isResolved) {
            Row(
                horizontalArrangement = Arrangement.spacedBy(8.dp),
                modifier = Modifier.fillMaxWidth()
            ) {
                OutlinedButton(
                    onClick = onUpdateStatusClick,
                    modifier = Modifier.weight(1f)
                ) {
                    Text("Update Status")
                }
                OutlinedButton(
                    onClick = onUpdateEtaClick,
                    modifier = Modifier.weight(1f)
                ) {
                    Text("Update ETA")
                }
            }
            Spacer(modifier = Modifier.height(8.dp))
            OutlinedButton(
                onClick = onAddWorkNoteClick,
                modifier = Modifier.fillMaxWidth()
            ) {
                Text("Add Work Note")
            }
            Spacer(modifier = Modifier.height(8.dp))
            Button(
                onClick = onResolveClick,
                modifier = Modifier.fillMaxWidth()
            ) {
                Text("Resolve Complaint")
            }
        } else {
            Text(
                text = "This complaint has been resolved.",
                style = MaterialTheme.typography.bodyMedium,
                color = MaterialTheme.colorScheme.primary
            )
        }

        Spacer(modifier = Modifier.height(24.dp))
    }
}
