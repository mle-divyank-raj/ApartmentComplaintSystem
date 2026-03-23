package com.acls.staff.ui.screens.mytasks

import android.content.Context
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.acls.staff.data.remote.toApiException
import com.acls.staff.domain.model.Staff
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

sealed class MyTasksUiState {
    data object Loading : MyTasksUiState()
    data class Success(val staff: Staff) : MyTasksUiState()
    data class Error(val message: String) : MyTasksUiState()
}

@HiltViewModel
class MyTasksViewModel @Inject constructor(
    private val staffRepository: StaffRepository,
    private val sessionManager: SessionManager,
    private val gson: Gson,
    @ApplicationContext private val appContext: Context
) : ViewModel() {

    private val _uiState = MutableStateFlow<MyTasksUiState>(MyTasksUiState.Loading)
    val uiState: StateFlow<MyTasksUiState> = _uiState

    init {
        loadProfile()
    }

    fun loadProfile() {
        viewModelScope.launch {
            _uiState.value = MyTasksUiState.Loading
            _uiState.value = try {
                val staff = staffRepository.getMyProfile()
                MyTasksUiState.Success(staff)
            } catch (e: HttpException) {
                MyTasksUiState.Error(ErrorCodeMapper.getMessage(appContext, e.toApiException(gson)))
            } catch (e: Exception) {
                MyTasksUiState.Error(ErrorCodeMapper.getMessage(appContext, "System.InternalError"))
            }
        }
    }

    fun signOut() {
        viewModelScope.launch {
            sessionManager.clearSession()
        }
    }
}
