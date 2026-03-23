import Foundation

// MARK: - TaskDetailViewModel

@MainActor
final class TaskDetailViewModel: ObservableObject {
    enum UiState {
        case idle
        case loading
        case success(complaint: ComplaintDTO)
        case error(message: String)
    }

    @Published private(set) var uiState: UiState = .idle
    @Published private(set) var isUpdatingStatus = false
    @Published var statusErrorMessage: String?

    private let complaintRepository: ComplaintRepository

    init(complaintRepository: ComplaintRepository = ComplaintRepository()) {
        self.complaintRepository = complaintRepository
    }

    func loadComplaint(id: Int, token: String) async {
        uiState = .loading
        do {
            let complaint = try await complaintRepository.getComplaint(id: id, token: token)
            uiState = .success(complaint: complaint)
        } catch let error as APIError {
            uiState = .error(message: ErrorCodeMapper.message(for: error))
        } catch {
            uiState = .error(message: ErrorCodeMapper.message(for: .networkError(error)))
        }
    }

    func updateStatus(complaintId: Int, newStatus: TicketStatus, token: String) async {
        isUpdatingStatus = true
        statusErrorMessage = nil
        do {
            let updated = try await complaintRepository.updateStatus(
                complaintId: complaintId,
                status: newStatus,
                token: token
            )
            uiState = .success(complaint: updated)
        } catch let error as APIError {
            statusErrorMessage = ErrorCodeMapper.message(for: error)
        } catch {
            statusErrorMessage = ErrorCodeMapper.message(for: .networkError(error))
        }
        isUpdatingStatus = false
    }
}
