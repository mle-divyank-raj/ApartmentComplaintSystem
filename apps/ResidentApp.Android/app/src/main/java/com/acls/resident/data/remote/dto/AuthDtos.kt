package com.acls.resident.data.remote.dto

import com.google.gson.annotations.SerializedName

data class RegisterResidentRequestDto(
    @SerializedName("invitationToken") val invitationToken: String,
    @SerializedName("email") val email: String,
    @SerializedName("password") val password: String,
    @SerializedName("firstName") val firstName: String,
    @SerializedName("lastName") val lastName: String,
    @SerializedName("phone") val phone: String? = null
)

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
