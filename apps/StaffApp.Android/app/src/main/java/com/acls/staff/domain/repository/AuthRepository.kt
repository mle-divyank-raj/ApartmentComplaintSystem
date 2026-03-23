package com.acls.staff.domain.repository

import com.acls.staff.domain.model.AuthToken

interface AuthRepository {
    suspend fun login(email: String, password: String): AuthToken
}
