package com.acls.staff.data.repository

import com.acls.staff.data.remote.ApiService
import com.acls.staff.data.remote.dto.LoginRequestDto
import com.acls.staff.domain.model.AuthToken
import com.acls.staff.domain.repository.AuthRepository
import com.acls.staff.session.SessionManager
import javax.inject.Inject

class AuthRepositoryImpl @Inject constructor(
    private val apiService: ApiService,
    private val sessionManager: SessionManager
) : AuthRepository {

    override suspend fun login(email: String, password: String): AuthToken {
        val dto = apiService.login(LoginRequestDto(email = email, password = password))
        sessionManager.saveSession(
            accessToken = dto.accessToken,
            userId = dto.userId,
            role = dto.role
        )
        return AuthToken(
            accessToken = dto.accessToken,
            expiresAt = dto.expiresAt,
            userId = dto.userId,
            role = dto.role,
            propertyId = dto.propertyId
        )
    }
}
