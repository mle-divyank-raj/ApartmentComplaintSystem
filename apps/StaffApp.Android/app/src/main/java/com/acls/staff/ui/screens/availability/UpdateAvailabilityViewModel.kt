package com.acls.staff.ui.screens.availability

import android.content.Context
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.acls.staff.data.remote.toApiException
import com.acls.staff.domain.repository.StaffRepository
import com.acls.staff.session.SessionManager
import com.acls.staff.ui.ErrorCodeMapper
import com.google.gson.Gson
import dagger.hilt.android.lifecycle.HiltViewModel
import dagger.hilt.android.qualifiers.ApplicationContext
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.launch
import retrofit2.HttpException
import javax.inject.Inject

sealed class UpdateAvailabilityUiState {
    data object Idle : UpdateAvailabilityUiState()
    data object Loading : UpdateAvailabilityUiState()
    data object Success : UpdateAvailabilityUiState()
    data class Error(val message: String) : UpdateAvailabilityUiState()
}

/** Availability values that staff can manually set. BUSY is system-assigned and excluded. */
enum class AvailabilityOption(val apiValue: String, val displayName: String) {
    AVAILABLE("AVAILABLE", "Available"),
    ON_BREAK("ON_BREAK", "On Break"),
    OFF_DUTY("OFF_DUTY", "Off Duty")
}

@HiltViewModel
class UpdateAvailabilityViewModel @Inject constructor(
    private val staffRepository: StaffRepository,
    private val sessionManager: SessionManager,
    private val gson: Gson,
    @ApplicationContext private val appContext: Context
) : ViewModel() {

    private val _uiState = MutableStateFlow<UpdateAvailabilityUiState>(UpdateAvailabilityUiState.Idle)
    val uiState: StateFlow<UpdateAvailabilityUiState> = _uiState

    fun updateAvailability(option: AvailabilityOption) {
        val staffMemberId = sessionManager.getStaffMemberId() ?: run {
            _uiState.value = UpdateAvailabilityUiState.Error("Staff profile not loaded. Please refresh My Tasks.")
            return
        }
        viewModelScope.launch {
            _uiState.value = UpdateAvailabilityUiState.Loading
            _uiState.value = try {
                staffRepository.updateAvailability(staffMemberId, option.apiValue)
                UpdateAvailabilityUiState.Success
            } catch (e: HttpException) {
                UpdateAvailabilityUiState.Error(ErrorCodeMapper.getMessage(appContext, e.toApiException(gson)))
            } catch (e: Exception) {
                UpdateAvailabilityUiState.Error(ErrorCodeMapper.getMessage(appContext, "System.InternalError"))
            }
        }
    }

    fun resetState() {
        _uiState.value = UpdateAvailabilityUiState.Idle
    }
}
