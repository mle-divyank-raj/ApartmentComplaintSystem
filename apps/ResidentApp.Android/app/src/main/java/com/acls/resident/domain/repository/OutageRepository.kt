package com.acls.resident.domain.repository

import com.acls.resident.domain.model.Outage

interface OutageRepository {
    suspend fun getOutages(): List<Outage>
}
