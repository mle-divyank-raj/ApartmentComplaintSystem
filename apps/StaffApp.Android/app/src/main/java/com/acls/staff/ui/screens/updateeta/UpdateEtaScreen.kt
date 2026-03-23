package com.acls.staff.ui.screens.updateeta

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
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.material3.TopAppBar
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import androidx.hilt.navigation.compose.hiltViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import com.acls.staff.ui.components.LoadingScreen

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun UpdateEtaScreen(
    complaintId: Int,
    onSuccess: () -> Unit,
    onNavigateUp: () -> Unit,
    viewModel: UpdateEtaViewModel = hiltViewModel()
) {
    val uiState by viewModel.uiState.collectAsStateWithLifecycle()

    var etaText by rememberSaveable { mutableStateOf("") }

    LaunchedEffect(uiState) {
        if (uiState is UpdateEtaUiState.Success) {
            viewModel.resetState()
            onSuccess()
        }
    }

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text("Update ETA") },
                navigationIcon = {
                    IconButton(onClick = onNavigateUp) {
                        Icon(Icons.AutoMirrored.Filled.ArrowBack, contentDescription = "Back")
                    }
                }
            )
        }
    ) { innerPadding ->
        if (uiState is UpdateEtaUiState.Loading) {
            LoadingScreen(modifier = Modifier.padding(innerPadding))
        } else {
            Column(
                modifier = Modifier
                    .padding(innerPadding)
                    .padding(24.dp)
            ) {
                Text(
                    text = "Enter the estimated arrival time (ISO 8601 format, e.g. 2025-06-15T14:30:00):",
                    style = MaterialTheme.typography.bodyLarge
                )
                Spacer(modifier = Modifier.height(16.dp))
                OutlinedTextField(
                    value = etaText,
                    onValueChange = { etaText = it },
                    label = { Text("ETA (e.g. 2025-06-15T14:30:00)") },
                    singleLine = true,
                    modifier = Modifier.fillMaxWidth()
                )

                if (uiState is UpdateEtaUiState.Error) {
                    Spacer(modifier = Modifier.height(8.dp))
                    Text(
                        text = (uiState as UpdateEtaUiState.Error).message,
                        color = MaterialTheme.colorScheme.error,
                        style = MaterialTheme.typography.bodyMedium
                    )
                }

                Spacer(modifier = Modifier.height(24.dp))
                Button(
                    onClick = { viewModel.updateEta(etaText.trim()) },
                    enabled = etaText.isNotBlank(),
                    modifier = Modifier.fillMaxWidth()
                ) {
                    Text("Save ETA")
                }
            }
        }
    }
}
