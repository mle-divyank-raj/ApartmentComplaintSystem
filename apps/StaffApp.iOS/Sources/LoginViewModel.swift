import Foundation

// MARK: - LoginViewModel

@MainActor
final class LoginViewModel: ObservableObject {
    enum UiState {
        case idle
        case loading
        case error(message: String)
    }

    @Published private(set) var uiState: UiState = .idle

    private let repository: AuthRepository

    init(repository: AuthRepository = AuthRepository()) {
        self.repository = repository
    }

    func login(email: String, password: String, tokenStore: TokenStore) async {
        uiState = .loading
        do {
            let response = try await repository.login(email: email, password: password)
            tokenStore.save(response: response)
            uiState = .idle
        } catch let error as APIError {
            uiState = .error(message: ErrorCodeMapper.message(for: error))
        } catch {
            uiState = .error(message: ErrorCodeMapper.message(for: .networkError(error)))
        }
    }
}
