import Foundation

// MARK: - UpdateEtaViewModel

@MainActor
final class UpdateEtaViewModel: ObservableObject {
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

    func updateEta(complaintId: Int, eta: Date, token: String) async {
        uiState = .loading
        do {
            _ = try await repository.updateEta(complaintId: complaintId, eta: eta, token: token)
            uiState = .success
        } catch let error as APIError {
            uiState = .error(message: ErrorCodeMapper.message(for: error))
        } catch {
            uiState = .error(message: ErrorCodeMapper.message(for: .networkError(error)))
        }
    }
}
