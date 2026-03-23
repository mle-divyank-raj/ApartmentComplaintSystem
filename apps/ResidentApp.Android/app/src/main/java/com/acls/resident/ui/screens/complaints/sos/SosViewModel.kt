package com.acls.resident.ui.screens.complaints.sos

import android.content.Context
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.acls.resident.data.remote.toApiException
import com.acls.resident.domain.model.Complaint
import com.acls.resident.domain.repository.ComplaintRepository
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

sealed class SosUiState {
    data object Idle : SosUiState()
    data object Loading : SosUiState()
    data class Success(val complaint: Complaint) : SosUiState()
    data class Error(val message: String) : SosUiState()
}

@HiltViewModel
class SosViewModel @Inject constructor(
    private val complaintRepository: ComplaintRepository,
    private val gson: Gson,
    @ApplicationContext private val appContext: Context
) : ViewModel() {

    private val _uiState = MutableStateFlow<SosUiState>(SosUiState.Idle)
    val uiState: StateFlow<SosUiState> = _uiState.asStateFlow()

    fun triggerSos(title: String, description: String, permissionToEnter: Boolean) {
        viewModelScope.launch {
            _uiState.value = SosUiState.Loading
            _uiState.value = try {
                val complaint = complaintRepository.triggerSos(title, description, permissionToEnter)
                SosUiState.Success(complaint)
            } catch (e: HttpException) {
                SosUiState.Error(ErrorCodeMapper.getMessage(appContext, e.toApiException(gson)))
            } catch (e: Exception) {
                SosUiState.Error(ErrorCodeMapper.getMessage(appContext, "System.InternalError"))
            }
        }
    }

    fun resetState() {
        _uiState.value = SosUiState.Idle
    }
}
