package com.acls.staff.data.remote.dto

import com.google.gson.annotations.SerializedName

data class LoginRequestDto(
    @SerializedName("email") val email: String,
    @SerializedName("password") val password: String
)

data class AuthTokenResponseDto(
    @SerializedName("accessToken") val accessToken: String,
    @SerializedName("expiresAt") val expiresAt: String,
    @SerializedName("userId") val userId: Int,
    @SerializedName("role") val role: String,
    @SerializedName("propertyId") val propertyId: Int
)
