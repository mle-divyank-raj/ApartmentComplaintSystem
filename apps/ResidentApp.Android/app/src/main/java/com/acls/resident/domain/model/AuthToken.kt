package com.acls.resident.domain.model

data class AuthToken(
    val accessToken: String,
    val expiresAt: String,
    val userId: Int,
    val role: String,
    val propertyId: Int
)
