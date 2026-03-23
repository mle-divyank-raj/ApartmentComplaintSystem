package com.acls.resident.ui.screens.outages

import android.content.Context
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.acls.resident.data.remote.toApiException
import com.acls.resident.domain.model.Outage
import com.acls.resident.domain.repository.OutageRepository
import com.acls.resident.ui.ErrorCodeMapper
import com.google.gson.Gson
import dagger.hilt.android.lifecycle.HiltViewModel
import dagger.hilt.android.qualifiers.ApplicationContext
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch
import retrofit2.HttpException
import javax.inject.Inject

sealed class OutagesUiState {
    data object Loading : OutagesUiState()
    data class Success(val outages: List<Outage>) : OutagesUiState()
    data class Error(val message: String) : OutagesUiState()
}

@HiltViewModel
class OutagesViewModel @Inject constructor(
    private val outageRepository: OutageRepository,
    private val gson: Gson,
    @ApplicationContext private val appContext: Context
) : ViewModel() {

    private val _uiState = MutableStateFlow<OutagesUiState>(OutagesUiState.Loading)
    val uiState: StateFlow<OutagesUiState> = _uiState.asStateFlow()

    init {
        loadOutages()
    }

    fun loadOutages() {
        viewModelScope.launch {
            _uiState.value = OutagesUiState.Loading
            _uiState.value = try {
                OutagesUiState.Success(outageRepository.getOutages())
            } catch (e: HttpException) {
                OutagesUiState.Error(ErrorCodeMapper.getMessage(appContext, e.toApiException(gson)))
            } catch (e: Exception) {
                OutagesUiState.Error(ErrorCodeMapper.getMessage(appContext, "System.InternalError"))
            }
        }
    }
}
