import Foundation

// MARK: - RegisterViewModel

@MainActor
final class RegisterViewModel: ObservableObject {
    @Published private(set) var isLoading = false
    @Published private(set) var errorMessage: String?

    private let repository: AuthRepository

    init(repository: AuthRepository = AuthRepository()) {
        self.repository = repository
    }

    func register(
        invitationToken: String,
        email: String,
        password: String,
        firstName: String,
        lastName: String,
        phone: String,
        tokenStore: TokenStore
    ) async {
        guard !firstName.isEmpty, !lastName.isEmpty, !email.isEmpty, !password.isEmpty else {
            errorMessage = NSLocalizedString("error_validation_failed", comment: "")
            return
        }
        isLoading = true
        errorMessage = nil
        defer { isLoading = false }

        do {
            let response = try await repository.register(
                invitationToken: invitationToken,
                email: email,
                password: password,
                firstName: firstName,
                lastName: lastName,
                phone: phone.isEmpty ? nil : phone
            )
            tokenStore.save(response: response)
        } catch let error as APIError {
            errorMessage = ErrorCodeMapper.message(for: error)
        } catch {
            errorMessage = ErrorCodeMapper.message(for: .networkError(error))
        }
    }
}
