import Foundation

// MARK: - LoginViewModel

@MainActor
final class LoginViewModel: ObservableObject {
    @Published private(set) var isLoading = false
    @Published private(set) var errorMessage: String?

    private let repository: AuthRepository

    init(repository: AuthRepository = AuthRepository()) {
        self.repository = repository
    }

    func login(email: String, password: String, tokenStore: TokenStore) async {
        guard !email.isEmpty, !password.isEmpty else {
            errorMessage = NSLocalizedString("error_validation_failed", comment: "")
            return
        }
        isLoading = true
        errorMessage = nil
        defer { isLoading = false }

        do {
            let response = try await repository.login(email: email, password: password)
            tokenStore.save(response: response)
        } catch let error as APIError {
            errorMessage = ErrorCodeMapper.message(for: error)
        } catch {
            errorMessage = ErrorCodeMapper.message(for: .networkError(error))
        }
    }
}
