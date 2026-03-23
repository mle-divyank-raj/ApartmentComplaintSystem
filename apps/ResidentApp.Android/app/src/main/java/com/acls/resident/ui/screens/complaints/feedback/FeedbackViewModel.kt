package com.acls.resident.ui.screens.complaints.feedback

import android.content.Context
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.acls.resident.data.remote.toApiException
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

sealed class FeedbackUiState {
    data object Idle : FeedbackUiState()
    data object Loading : FeedbackUiState()
    data object Success : FeedbackUiState()
    data class Error(val message: String) : FeedbackUiState()
}

@HiltViewModel
class FeedbackViewModel @Inject constructor(
    private val complaintRepository: ComplaintRepository,
    private val gson: Gson,
    @ApplicationContext private val appContext: Context
) : ViewModel() {

    private val _uiState = MutableStateFlow<FeedbackUiState>(FeedbackUiState.Idle)
    val uiState: StateFlow<FeedbackUiState> = _uiState.asStateFlow()

    fun submitFeedback(complaintId: Int, rating: Int, comment: String?) {
        viewModelScope.launch {
            _uiState.value = FeedbackUiState.Loading
            _uiState.value = try {
                complaintRepository.submitFeedback(complaintId, rating, comment)
                FeedbackUiState.Success
            } catch (e: HttpException) {
                FeedbackUiState.Error(ErrorCodeMapper.getMessage(appContext, e.toApiException(gson)))
            } catch (e: Exception) {
                FeedbackUiState.Error(ErrorCodeMapper.getMessage(appContext, "System.InternalError"))
            }
        }
    }

    fun resetState() {
        _uiState.value = FeedbackUiState.Idle
    }
}
