package com.acls.staff.ui.screens.updatestatus

import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material3.Button
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.material3.TopAppBar
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import androidx.hilt.navigation.compose.hiltViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import com.acls.staff.ui.components.LoadingScreen

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun UpdateStatusScreen(
    complaintId: Int,
    onSuccess: () -> Unit,
    onNavigateUp: () -> Unit,
    viewModel: UpdateStatusViewModel = hiltViewModel()
) {
    val uiState by viewModel.uiState.collectAsStateWithLifecycle()

    LaunchedEffect(uiState) {
        if (uiState is UpdateStatusUiState.Success) {
            viewModel.resetState()
            onSuccess()
        }
    }

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text("Update Status") },
                navigationIcon = {
                    IconButton(onClick = onNavigateUp) {
                        Icon(Icons.AutoMirrored.Filled.ArrowBack, contentDescription = "Back")
                    }
                }
            )
        }
    ) { innerPadding ->
        if (uiState is UpdateStatusUiState.Loading) {
            LoadingScreen(modifier = Modifier.padding(innerPadding))
        } else {
            Column(modifier = Modifier
                .padding(innerPadding)
                .padding(24.dp)) {
                Text(
                    text = "Select the new status for this complaint:",
                    style = MaterialTheme.typography.bodyLarge
                )

                if (uiState is UpdateStatusUiState.Error) {
                    Spacer(modifier = Modifier.height(8.dp))
                    Text(
                        text = (uiState as UpdateStatusUiState.Error).message,
                        color = MaterialTheme.colorScheme.error,
                        style = MaterialTheme.typography.bodyMedium
                    )
                }

                Spacer(modifier = Modifier.height(24.dp))

                val status = viewModel.currentStatus?.uppercase() ?: ""

                when (status) {
                    "ASSIGNED" -> {
                        Button(
                            onClick = { viewModel.updateStatus("EN_ROUTE") },
                            modifier = Modifier.fillMaxWidth()
                        ) {
                            Text("Accept & En Route")
                        }
                    }
                    "EN_ROUTE" -> {
                        Button(
                            onClick = { viewModel.updateStatus("IN_PROGRESS") },
                            modifier = Modifier.fillMaxWidth()
                        ) {
                            Text("Start Work")
                        }
                    }
                    else -> {
                        Button(
                            onClick = { viewModel.updateStatus("EN_ROUTE") },
                            modifier = Modifier.fillMaxWidth()
                        ) {
                            Text("En Route")
                        }
                        Spacer(modifier = Modifier.height(12.dp))
                        OutlinedButton(
                            onClick = { viewModel.updateStatus("IN_PROGRESS") },
                            modifier = Modifier.fillMaxWidth()
                        ) {
                            Text("In Progress")
                        }
                    }
                }
            }
        }
    }
}
