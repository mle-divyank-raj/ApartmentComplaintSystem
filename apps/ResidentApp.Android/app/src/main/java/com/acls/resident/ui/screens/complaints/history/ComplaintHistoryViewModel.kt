package com.acls.resident.ui.screens.complaints.history

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

sealed class ComplaintHistoryUiState {
    data object Loading : ComplaintHistoryUiState()
    data class Success(val complaints: List<Complaint>, val hasMore: Boolean) : ComplaintHistoryUiState()
    data class Error(val message: String) : ComplaintHistoryUiState()
}

@HiltViewModel
class ComplaintHistoryViewModel @Inject constructor(
    private val complaintRepository: ComplaintRepository,
    private val gson: Gson,
    @ApplicationContext private val appContext: Context
) : ViewModel() {

    private val _uiState = MutableStateFlow<ComplaintHistoryUiState>(ComplaintHistoryUiState.Loading)
    val uiState: StateFlow<ComplaintHistoryUiState> = _uiState.asStateFlow()

    private var currentPage = 1
    private val pageSize = 20

    init {
        loadComplaints()
    }

    fun loadComplaints() {
        viewModelScope.launch {
            _uiState.value = ComplaintHistoryUiState.Loading
            _uiState.value = try {
                val page = complaintRepository.getMyComplaints(page = 1, pageSize = pageSize)
                currentPage = 1
                ComplaintHistoryUiState.Success(
                    complaints = page.items,
                    hasMore = page.totalCount > page.items.size
                )
            } catch (e: HttpException) {
                ComplaintHistoryUiState.Error(ErrorCodeMapper.getMessage(appContext, e.toApiException(gson)))
            } catch (e: Exception) {
                ComplaintHistoryUiState.Error(ErrorCodeMapper.getMessage(appContext, "System.InternalError"))
            }
        }
    }
}
