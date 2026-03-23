import Foundation

// MARK: - ComplaintListViewModel

@MainActor
final class ComplaintListViewModel: ObservableObject {
    enum UiState {
        case idle
        case loading
        case success(complaints: [ComplaintDTO])
        case error(message: String)
    }

    @Published private(set) var uiState: UiState = .idle

    private let repository: ComplaintRepository

    init(repository: ComplaintRepository = ComplaintRepository()) {
        self.repository = repository
    }

    func loadComplaints(token: String) async {
        uiState = .loading
        do {
            let page = try await repository.getMyComplaints(token: token)
            uiState = .success(complaints: page.items)
        } catch let error as APIError {
            uiState = .error(message: ErrorCodeMapper.message(for: error))
        } catch {
            uiState = .error(message: ErrorCodeMapper.message(for: .networkError(error)))
        }
    }
}
