package com.acls.resident.data.remote.dto

import com.google.gson.annotations.SerializedName

data class OutageDto(
    @SerializedName("outageId") val outageId: Int,
    @SerializedName("propertyId") val propertyId: Int,
    @SerializedName("title") val title: String,
    @SerializedName("outageType") val outageType: String,
    @SerializedName("description") val description: String,
    @SerializedName("startTime") val startTime: String,
    @SerializedName("endTime") val endTime: String,
    @SerializedName("declaredAt") val declaredAt: String,
    @SerializedName("notificationSentAt") val notificationSentAt: String?
)
