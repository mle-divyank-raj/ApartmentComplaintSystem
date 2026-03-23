package com.acls.staff.ui.screens.complaintdetail

import android.content.Context
import androidx.lifecycle.SavedStateHandle
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.acls.staff.data.remote.toApiException
import com.acls.staff.domain.model.Complaint
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

sealed class ComplaintDetailUiState {
    data object Loading : ComplaintDetailUiState()
    data class Success(val complaint: Complaint) : ComplaintDetailUiState()
    data class Error(val message: String) : ComplaintDetailUiState()
}

@HiltViewModel
class ComplaintDetailViewModel @Inject constructor(
    private val complaintRepository: ComplaintRepository,
    savedStateHandle: SavedStateHandle,
    private val gson: Gson,
    @ApplicationContext private val appContext: Context
) : ViewModel() {

    private val complaintId: Int = checkNotNull(savedStateHandle["complaintId"])

    private val _uiState = MutableStateFlow<ComplaintDetailUiState>(ComplaintDetailUiState.Loading)
    val uiState: StateFlow<ComplaintDetailUiState> = _uiState

    init {
        loadComplaint()
    }

    fun loadComplaint() {
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
