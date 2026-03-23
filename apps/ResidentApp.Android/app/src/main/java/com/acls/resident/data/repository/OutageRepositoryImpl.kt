package com.acls.resident.data.repository

import com.acls.resident.data.remote.ApiService
import com.acls.resident.domain.model.Outage
import com.acls.resident.domain.repository.OutageRepository
import javax.inject.Inject

class OutageRepositoryImpl @Inject constructor(
    private val apiService: ApiService
) : OutageRepository {

    override suspend fun getOutages(): List<Outage> =
        apiService.getOutages().map { dto ->
            Outage(
                outageId = dto.outageId,
                title = dto.title,
                outageType = dto.outageType,
                description = dto.description,
                startTime = dto.startTime,
                endTime = dto.endTime,
                declaredAt = dto.declaredAt
            )
        }
}
