package com.acls.resident.data.repository

import com.acls.resident.data.remote.ApiService
import com.acls.resident.data.remote.dto.LoginRequestDto
import com.acls.resident.data.remote.dto.RegisterResidentRequestDto
import com.acls.resident.domain.model.AuthToken
import com.acls.resident.domain.repository.AuthRepository
import com.acls.resident.session.SessionManager
import javax.inject.Inject

class AuthRepositoryImpl @Inject constructor(
    private val apiService: ApiService,
    private val sessionManager: SessionManager
) : AuthRepository {

    override suspend fun register(
        invitationToken: String,
        email: String,
        password: String,
        firstName: String,
        lastName: String,
        phone: String?
    ): AuthToken {
        val dto = apiService.register(
            RegisterResidentRequestDto(
                invitationToken = invitationToken,
                email = email,
                password = password,
                firstName = firstName,
                lastName = lastName,
                phone = phone
            )
        )
        val token = AuthToken(
            accessToken = dto.accessToken,
            expiresAt = dto.expiresAt,
            userId = dto.userId,
            role = dto.role,
            propertyId = dto.propertyId
        )
        sessionManager.saveSession(token.accessToken, token.userId, token.role, token.propertyId)
        return token
    }

    override suspend fun login(email: String, password: String): AuthToken {
        val dto = apiService.login(LoginRequestDto(email = email, password = password))
        val token = AuthToken(
            accessToken = dto.accessToken,
            expiresAt = dto.expiresAt,
            userId = dto.userId,
            role = dto.role,
            propertyId = dto.propertyId
        )
        sessionManager.saveSession(token.accessToken, token.userId, token.role, token.propertyId)
        return token
    }
}
