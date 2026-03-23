package com.acls.resident.domain.model

data class Outage(
    val outageId: Int,
    val title: String,
    val outageType: String,
    val description: String,
    val startTime: String,
    val endTime: String,
    val declaredAt: String
)
