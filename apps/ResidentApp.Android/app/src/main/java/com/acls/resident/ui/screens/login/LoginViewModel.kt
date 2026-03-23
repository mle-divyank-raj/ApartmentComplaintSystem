package com.acls.resident.ui.screens.login

import android.content.Context
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.acls.resident.data.remote.toApiException
import com.acls.resident.domain.repository.AuthRepository
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

sealed class LoginUiState {
    data object Idle : LoginUiState()
    data object Loading : LoginUiState()
    data object Success : LoginUiState()
    data class Error(val message: String) : LoginUiState()
}

@HiltViewModel
class LoginViewModel @Inject constructor(
    private val authRepository: AuthRepository,
    private val gson: Gson,
    @ApplicationContext private val appContext: Context
) : ViewModel() {

    private val _uiState = MutableStateFlow<LoginUiState>(LoginUiState.Idle)
    val uiState: StateFlow<LoginUiState> = _uiState.asStateFlow()

    fun login(email: String, password: String) {
        viewModelScope.launch {
            _uiState.value = LoginUiState.Loading
            _uiState.value = try {
                authRepository.login(email, password)
                LoginUiState.Success
            } catch (e: HttpException) {
                LoginUiState.Error(ErrorCodeMapper.getMessage(appContext, e.toApiException(gson)))
            } catch (e: Exception) {
                LoginUiState.Error(ErrorCodeMapper.getMessage(appContext, "System.InternalError"))
            }
        }
    }

    fun resetState() {
        _uiState.value = LoginUiState.Idle
    }
}
