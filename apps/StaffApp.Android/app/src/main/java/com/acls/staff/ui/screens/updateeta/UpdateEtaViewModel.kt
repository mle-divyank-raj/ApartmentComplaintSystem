package com.acls.staff.ui.screens.updateeta

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

sealed class UpdateEtaUiState {
    data object Idle : UpdateEtaUiState()
    data object Loading : UpdateEtaUiState()
    data object Success : UpdateEtaUiState()
    data class Error(val message: String) : UpdateEtaUiState()
}

@HiltViewModel
class UpdateEtaViewModel @Inject constructor(
    private val complaintRepository: ComplaintRepository,
    savedStateHandle: SavedStateHandle,
    private val gson: Gson,
    @ApplicationContext private val appContext: Context
) : ViewModel() {

    private val complaintId: Int = checkNotNull(savedStateHandle["complaintId"])

    private val _uiState = MutableStateFlow<UpdateEtaUiState>(UpdateEtaUiState.Idle)
    val uiState: StateFlow<UpdateEtaUiState> = _uiState

    fun updateEta(eta: String) {
        viewModelScope.launch {
            _uiState.value = UpdateEtaUiState.Loading
            _uiState.value = try {
                complaintRepository.updateEta(complaintId, eta)
                UpdateEtaUiState.Success
            } catch (e: HttpException) {
                UpdateEtaUiState.Error(ErrorCodeMapper.getMessage(appContext, e.toApiException(gson)))
            } catch (e: Exception) {
                UpdateEtaUiState.Error(ErrorCodeMapper.getMessage(appContext, "System.InternalError"))
            }
        }
    }

    fun resetState() {
        _uiState.value = UpdateEtaUiState.Idle
    }
}
