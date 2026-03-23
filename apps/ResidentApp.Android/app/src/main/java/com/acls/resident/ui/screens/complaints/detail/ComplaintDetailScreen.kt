package com.acls.resident.ui.screens.complaints.detail

import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.lazy.LazyRow
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material3.Button
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.material3.TopAppBar
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.layout.ContentScale
import androidx.compose.ui.unit.dp
import androidx.hilt.navigation.compose.hiltViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import coil.compose.AsyncImage
import com.acls.resident.ui.components.ErrorScreen
import com.acls.resident.ui.components.LoadingScreen

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun ComplaintDetailScreen(
    complaintId: Int,
    onNavigateToFeedback: (Int) -> Unit,
    onNavigateBack: () -> Unit,
    viewModel: ComplaintDetailViewModel = hiltViewModel()
) {
    val uiState by viewModel.uiState.collectAsStateWithLifecycle()

    LaunchedEffect(complaintId) {
        viewModel.loadComplaint(complaintId)
    }

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text("Complaint #$complaintId") },
                navigationIcon = {
                    IconButton(onClick = onNavigateBack) {
                        Icon(Icons.AutoMirrored.Filled.ArrowBack, contentDescription = "Back")
                    }
                }
            )
        }
    ) { padding ->
        when (val state = uiState) {
            is ComplaintDetailUiState.Loading -> LoadingScreen(Modifier.padding(padding))
            is ComplaintDetailUiState.Error -> ErrorScreen(
                message = state.message,
                onRetry = { viewModel.loadComplaint(complaintId) },
                modifier = Modifier.padding(padding)
            )
            is ComplaintDetailUiState.Success -> {
                val complaint = state.complaint
                Column(
                    modifier = Modifier
                        .fillMaxSize()
                        .padding(padding)
                        .padding(16.dp)
                        .verticalScroll(rememberScrollState())
                ) {
                    Text(complaint.title, style = MaterialTheme.typography.titleLarge)
                    Spacer(Modifier.height(4.dp))
                    Text("Status: ${complaint.status}", style = MaterialTheme.typography.bodyMedium)
                    Text("Urgency: ${complaint.urgency}", style = MaterialTheme.typography.bodyMedium)
                    Text("Category: ${complaint.category}", style = MaterialTheme.typography.bodyMedium)
                    complaint.unitNumber?.let { Text("Unit: $it", style = MaterialTheme.typography.bodyMedium) }
                    complaint.buildingName?.let { Text("Building: $it", style = MaterialTheme.typography.bodyMedium) }

                    // TAT display for resolved complaints
                    if (complaint.status == "RESOLVED" && complaint.tat != null) {
                        val hours = complaint.tat.toInt()
                        val minutes = ((complaint.tat - hours) * 60).toInt()
                        val tatText = if (hours > 0) "Resolved in ${hours}h ${minutes}m" else "Resolved in ${minutes}m"
                        Spacer(Modifier.height(4.dp))
                        Text(tatText, style = MaterialTheme.typography.bodyMedium, color = MaterialTheme.colorScheme.primary)
                    }

                    Spacer(Modifier.height(12.dp))
                    HorizontalDivider()
                    Spacer(Modifier.height(12.dp))

                    Text("Description", style = MaterialTheme.typography.titleLarge)
                    Text(complaint.description, style = MaterialTheme.typography.bodyLarge)

                    // Media thumbnails
                    if (complaint.mediaUrls.isNotEmpty()) {
                        Spacer(Modifier.height(12.dp))
                        HorizontalDivider()
                        Spacer(Modifier.height(8.dp))
                        Text("Photos", style = MaterialTheme.typography.titleMedium)
                        Spacer(Modifier.height(8.dp))
                        LazyRow(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                            items(complaint.mediaUrls) { url ->
                                AsyncImage(
                                    model = url,
                                    contentDescription = "Complaint photo",
                                    modifier = Modifier
                                        .size(120.dp)
                                        .clip(RoundedCornerShape(8.dp)),
                                    contentScale = ContentScale.Crop
                                )
                            }
                        }
                    }

                    if (complaint.assignedStaffName != null) {
                        Spacer(Modifier.height(12.dp))
                        Text("Assigned to: ${complaint.assignedStaffName}", style = MaterialTheme.typography.bodyMedium)
                        complaint.eta?.let { Text("ETA: ${it.take(16)}", style = MaterialTheme.typography.bodyMedium) }
                    }

                    if (complaint.workNotes.isNotEmpty()) {
                        Spacer(Modifier.height(12.dp))
                        HorizontalDivider()
                        Spacer(Modifier.height(8.dp))
                        Text("Work Notes", style = MaterialTheme.typography.titleLarge)
                        complaint.workNotes.forEach { note ->
                            Text(
                                "• ${note.staffMemberName}: ${note.content}",
                                style = MaterialTheme.typography.bodyMedium
                            )
                        }
                    }

                    if (complaint.status == "RESOLVED" && complaint.residentRating == null) {
                        Spacer(Modifier.height(16.dp))
                        Button(
                            onClick = { onNavigateToFeedback(complaintId) },
                            modifier = Modifier.fillMaxWidth()
                        ) {
                            Text("Leave Feedback")
                        }
                    }

                    if (complaint.residentRating != null) {
                        Spacer(Modifier.height(12.dp))
                        Text(
                            "Your rating: ${complaint.residentRating}/5",
                            style = MaterialTheme.typography.bodyMedium
                        )
                        complaint.residentFeedbackComment?.let {
                            Text("Comment: $it", style = MaterialTheme.typography.bodyMedium)
                        }
                    }
                }
            }
        }
    }
}
