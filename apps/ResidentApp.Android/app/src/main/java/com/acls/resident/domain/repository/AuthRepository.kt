package com.acls.resident.domain.repository

import com.acls.resident.domain.model.AuthToken

interface AuthRepository {
    suspend fun register(
        invitationToken: String,
        email: String,
        password: String,
        firstName: String,
        lastName: String,
        phone: String?
    ): AuthToken

    suspend fun login(email: String, password: String): AuthToken
}
