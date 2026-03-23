package com.acls.staff.ui.screens.updatestatus

import android.content.Context
import androidx.lifecycle.SavedStateHandle
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.acls.staff.data.remote.toApiException
import com.acls.staff.domain.repository.ComplaintRepository
import com.acls.staff.ui.ErrorCodeMapper
import com.google.gson.Gson
import dagger.hilt.android.lifecycle.HiltViewModel
import dagger.hilt.android.qualifiers.ApplicationContext
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.launch
import retrofit2.HttpException
import javax.inject.Inject

sealed class UpdateStatusUiState {
    data object Idle : UpdateStatusUiState()
    data object Loading : UpdateStatusUiState()
    data object Success : UpdateStatusUiState()
    data class Error(val message: String) : UpdateStatusUiState()
}

@HiltViewModel
class UpdateStatusViewModel @Inject constructor(
    private val complaintRepository: ComplaintRepository,
    savedStateHandle: SavedStateHandle,
    private val gson: Gson,
    @ApplicationContext private val appContext: Context
) : ViewModel() {

    private val complaintId: Int = checkNotNull(savedStateHandle["complaintId"])
    val currentStatus: String? = savedStateHandle["currentStatus"]

    private val _uiState = MutableStateFlow<UpdateStatusUiState>(UpdateStatusUiState.Idle)
    val uiState: StateFlow<UpdateStatusUiState> = _uiState

    fun updateStatus(status: String) {
        viewModelScope.launch {
            _uiState.value = UpdateStatusUiState.Loading
            _uiState.value = try {
                complaintRepository.updateStatus(complaintId, status)
                UpdateStatusUiState.Success
            } catch (e: HttpException) {
                UpdateStatusUiState.Error(ErrorCodeMapper.getMessage(appContext, e.toApiException(gson)))
            } catch (e: Exception) {
                UpdateStatusUiState.Error(ErrorCodeMapper.getMessage(appContext, "System.InternalError"))
            }
        }
    }

    fun resetState() {
        _uiState.value = UpdateStatusUiState.Idle
    }
}
