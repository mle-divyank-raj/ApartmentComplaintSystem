package com.acls.resident.ui.screens.complaints.submit

import android.content.Context
import android.net.Uri
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

sealed class SubmitComplaintUiState {
    data object Idle : SubmitComplaintUiState()
    data object Loading : SubmitComplaintUiState()
    data class Success(val complaint: Complaint) : SubmitComplaintUiState()
    data class Error(val message: String) : SubmitComplaintUiState()
}

@HiltViewModel
class SubmitComplaintViewModel @Inject constructor(
    private val complaintRepository: ComplaintRepository,
    private val gson: Gson,
    @ApplicationContext private val appContext: Context
) : ViewModel() {

    private val _uiState = MutableStateFlow<SubmitComplaintUiState>(SubmitComplaintUiState.Idle)
    val uiState: StateFlow<SubmitComplaintUiState> = _uiState.asStateFlow()

    fun submit(
        title: String,
        description: String,
        category: String,
        urgency: String,
        permissionToEnter: Boolean,
        mediaUris: List<Uri>
    ) {
        viewModelScope.launch {
            _uiState.value = SubmitComplaintUiState.Loading
            _uiState.value = try {
                val complaint = complaintRepository.submitComplaint(
                    title = title,
                    description = description,
                    category = category,
                    urgency = urgency,
                    permissionToEnter = permissionToEnter,
                    mediaUris = mediaUris
                )
                SubmitComplaintUiState.Success(complaint)
            } catch (e: HttpException) {
                SubmitComplaintUiState.Error(ErrorCodeMapper.getMessage(appContext, e.toApiException(gson)))
            } catch (e: Exception) {
                SubmitComplaintUiState.Error(ErrorCodeMapper.getMessage(appContext, "System.InternalError"))
            }
        }
    }

    fun resetState() {
        _uiState.value = SubmitComplaintUiState.Idle
    }
}
