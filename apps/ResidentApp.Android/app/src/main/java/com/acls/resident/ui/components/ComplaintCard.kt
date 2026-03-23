package com.acls.resident.ui.components

import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import com.acls.resident.domain.model.Complaint

@Composable
fun ComplaintCard(complaint: Complaint, onClick: () -> Unit) {
    Card(
        modifier = Modifier
            .fillMaxWidth()
            .padding(horizontal = 16.dp, vertical = 6.dp)
            .clickable(onClick = onClick),
        elevation = CardDefaults.cardElevation(defaultElevation = 2.dp)
    ) {
        Column(modifier = Modifier.padding(16.dp)) {
            Text(text = complaint.title, style = MaterialTheme.typography.titleLarge)
            Spacer(Modifier.height(4.dp))
            Row {
                StatusChip(label = complaint.status)
                Spacer(Modifier.width(8.dp))
                UrgencyChip(label = complaint.urgency)
            }
            Spacer(Modifier.height(4.dp))
            Text(
                text = complaint.category,
                style = MaterialTheme.typography.bodyMedium,
                color = MaterialTheme.colorScheme.onSurfaceVariant
            )
            Text(
                text = complaint.createdAt.take(10),
                style = MaterialTheme.typography.labelSmall,
                color = MaterialTheme.colorScheme.onSurfaceVariant
            )
        }
    }
}

@Composable
private fun StatusChip(label: String) {
    val color = when (label) {
        "OPEN" -> MaterialTheme.colorScheme.primary
        "ASSIGNED" -> MaterialTheme.colorScheme.secondary
        "EN_ROUTE", "IN_PROGRESS" -> MaterialTheme.colorScheme.tertiary
        "RESOLVED", "CLOSED" -> MaterialTheme.colorScheme.outline
        else -> MaterialTheme.colorScheme.onSurfaceVariant
    }
    Text(
        text = label,
        style = MaterialTheme.typography.labelSmall,
        color = color
    )
}

@Composable
private fun UrgencyChip(label: String) {
    val color = when (label) {
        "SOS_EMERGENCY" -> MaterialTheme.colorScheme.error
        "HIGH" -> MaterialTheme.colorScheme.secondary
        "MEDIUM" -> MaterialTheme.colorScheme.primary
        else -> MaterialTheme.colorScheme.onSurfaceVariant
    }
    Text(text = label, style = MaterialTheme.typography.labelSmall, color = color)
}
