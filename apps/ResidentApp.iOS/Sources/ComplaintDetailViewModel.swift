import Foundation

// MARK: - ComplaintDetailViewModel

@MainActor
final class ComplaintDetailViewModel: ObservableObject {
    enum UiState {
        case idle
        case loading
        case success(complaint: ComplaintDTO)
        case error(message: String)
    }

    @Published private(set) var uiState: UiState = .idle

    private let repository: ComplaintRepository

    init(repository: ComplaintRepository = ComplaintRepository()) {
        self.repository = repository
    }

    func loadComplaint(id: Int, token: String) async {
        uiState = .loading
        do {
            let complaint = try await repository.getComplaint(id: id, token: token)
            uiState = .success(complaint: complaint)
        } catch let error as APIError {
            uiState = .error(message: ErrorCodeMapper.message(for: error))
        } catch {
            uiState = .error(message: ErrorCodeMapper.message(for: .networkError(error)))
        }
    }
}
