import Foundation

// MARK: - AddWorkNoteViewModel

@MainActor
final class AddWorkNoteViewModel: ObservableObject {
    enum UiState {
        case idle
        case loading
        case success
        case error(message: String)
    }

    @Published private(set) var uiState: UiState = .idle

    private let repository: ComplaintRepository

    init(repository: ComplaintRepository = ComplaintRepository()) {
        self.repository = repository
    }

    func addWorkNote(complaintId: Int, note: String, token: String) async {
        uiState = .loading
        do {
            _ = try await repository.addWorkNote(complaintId: complaintId, note: note, token: token)
            uiState = .success
        } catch let error as APIError {
            uiState = .error(message: ErrorCodeMapper.message(for: error))
        } catch {
            uiState = .error(message: ErrorCodeMapper.message(for: .networkError(error)))
        }
    }
}
