package com.acls.resident.ui.screens.complaints.detail

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

sealed class ComplaintDetailUiState {
    data object Loading : ComplaintDetailUiState()
    data class Success(val complaint: Complaint) : ComplaintDetailUiState()
    data class Error(val message: String) : ComplaintDetailUiState()
}

@HiltViewModel
class ComplaintDetailViewModel @Inject constructor(
    private val complaintRepository: ComplaintRepository,
    private val gson: Gson,
    @ApplicationContext private val appContext: Context
) : ViewModel() {

    private val _uiState = MutableStateFlow<ComplaintDetailUiState>(ComplaintDetailUiState.Loading)
    val uiState: StateFlow<ComplaintDetailUiState> = _uiState.asStateFlow()

    fun loadComplaint(complaintId: Int) {
        viewModelScope.launch {
            _uiState.value = ComplaintDetailUiState.Loading
            _uiState.value = try {
                ComplaintDetailUiState.Success(complaintRepository.getComplaint(complaintId))
            } catch (e: HttpException) {
                ComplaintDetailUiState.Error(ErrorCodeMapper.getMessage(appContext, e.toApiException(gson)))
            } catch (e: Exception) {
                ComplaintDetailUiState.Error(ErrorCodeMapper.getMessage(appContext, "System.InternalError"))
            }
        }
    }
}
