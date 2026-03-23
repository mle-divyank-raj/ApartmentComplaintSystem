import Foundation

// MARK: - SosViewModel

@MainActor
final class SosViewModel: ObservableObject {
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

    func triggerSos(
        title: String,
        description: String,
        permissionToEnter: Bool,
        token: String
    ) async {
        uiState = .loading
        do {
            let complaint = try await repository.triggerSos(
                title: title,
                description: description,
                permissionToEnter: permissionToEnter,
                token: token
            )
            uiState = .success(complaint: complaint)
        } catch let error as APIError {
            uiState = .error(message: ErrorCodeMapper.message(for: error))
        } catch {
            uiState = .error(message: ErrorCodeMapper.message(for: .networkError(error)))
        }
    }
}
