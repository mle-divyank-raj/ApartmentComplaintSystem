package com.acls.staff.ui.screens.resolve

import android.content.Context
import android.net.Uri
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

sealed class ResolveComplaintUiState {
    data object Idle : ResolveComplaintUiState()
    data object Loading : ResolveComplaintUiState()
    data object Success : ResolveComplaintUiState()
    data class Error(val message: String) : ResolveComplaintUiState()
}

@HiltViewModel
class ResolveComplaintViewModel @Inject constructor(
    private val complaintRepository: ComplaintRepository,
    savedStateHandle: SavedStateHandle,
    private val gson: Gson,
    @ApplicationContext private val appContext: Context
) : ViewModel() {

    private val complaintId: Int = checkNotNull(savedStateHandle["complaintId"])

    private val _uiState = MutableStateFlow<ResolveComplaintUiState>(ResolveComplaintUiState.Idle)
    val uiState: StateFlow<ResolveComplaintUiState> = _uiState

    fun resolve(resolutionNotes: String, photos: List<Uri>) {
        viewModelScope.launch {
            _uiState.value = ResolveComplaintUiState.Loading
            _uiState.value = try {
                complaintRepository.resolveComplaint(complaintId, resolutionNotes, photos)
                ResolveComplaintUiState.Success
            } catch (e: HttpException) {
                ResolveComplaintUiState.Error(ErrorCodeMapper.getMessage(appContext, e.toApiException(gson)))
            } catch (e: Exception) {
                ResolveComplaintUiState.Error(ErrorCodeMapper.getMessage(appContext, "System.InternalError"))
            }
        }
    }

    fun resetState() {
        _uiState.value = ResolveComplaintUiState.Idle
    }
}
