package com.acls.staff.ui.screens.worknote

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

sealed class AddWorkNoteUiState {
    data object Idle : AddWorkNoteUiState()
    data object Loading : AddWorkNoteUiState()
    data object Success : AddWorkNoteUiState()
    data class Error(val message: String) : AddWorkNoteUiState()
}

@HiltViewModel
class AddWorkNoteViewModel @Inject constructor(
    private val complaintRepository: ComplaintRepository,
    savedStateHandle: SavedStateHandle,
    private val gson: Gson,
    @ApplicationContext private val appContext: Context
) : ViewModel() {

    private val complaintId: Int = checkNotNull(savedStateHandle["complaintId"])

    private val _uiState = MutableStateFlow<AddWorkNoteUiState>(AddWorkNoteUiState.Idle)
    val uiState: StateFlow<AddWorkNoteUiState> = _uiState

    fun addWorkNote(content: String) {
        viewModelScope.launch {
            _uiState.value = AddWorkNoteUiState.Loading
            _uiState.value = try {
                complaintRepository.addWorkNote(complaintId, content)
                AddWorkNoteUiState.Success
            } catch (e: HttpException) {
                AddWorkNoteUiState.Error(ErrorCodeMapper.getMessage(appContext, e.toApiException(gson)))
            } catch (e: Exception) {
                AddWorkNoteUiState.Error(ErrorCodeMapper.getMessage(appContext, "System.InternalError"))
            }
        }
    }

    fun resetState() {
        _uiState.value = AddWorkNoteUiState.Idle
    }
}
