package com.acls.resident.ui.screens.complaints.history

import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.material3.TopAppBar
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.ui.Modifier
import androidx.hilt.navigation.compose.hiltViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import com.acls.resident.ui.components.ComplaintCard
import com.acls.resident.ui.components.ErrorScreen
import com.acls.resident.ui.components.LoadingScreen

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun ComplaintHistoryScreen(
    onComplaintClick: (Int) -> Unit,
    onNavigateBack: () -> Unit,
    viewModel: ComplaintHistoryViewModel = hiltViewModel()
) {
    val uiState by viewModel.uiState.collectAsStateWithLifecycle()

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text("My Complaints") },
                navigationIcon = {
                    IconButton(onClick = onNavigateBack) {
                        Icon(Icons.AutoMirrored.Filled.ArrowBack, contentDescription = "Back")
                    }
                }
            )
        }
    ) { padding ->
        when (val state = uiState) {
            is ComplaintHistoryUiState.Loading -> LoadingScreen(Modifier.padding(padding))
            is ComplaintHistoryUiState.Error -> ErrorScreen(
                message = state.message,
                onRetry = viewModel::loadComplaints,
                modifier = Modifier.padding(padding)
            )
            is ComplaintHistoryUiState.Success -> {
                if (state.complaints.isEmpty()) {
                    ErrorScreen(
                        message = "No complaints yet.",
                        modifier = Modifier.padding(padding)
                    )
                } else {
                    LazyColumn(
                        modifier = Modifier.fillMaxSize().padding(padding)
                    ) {
                        items(state.complaints, key = { it.complaintId }) { complaint ->
                            ComplaintCard(
                                complaint = complaint,
                                onClick = { onComplaintClick(complaint.complaintId) }
                            )
                        }
                    }
                }
            }
        }
    }
}
