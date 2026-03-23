import Foundation

// MARK: - AuthRepository

final class AuthRepository {
    private let client: APIClient

    init(client: APIClient = .shared) {
        self.client = client
    }

    func login(email: String, password: String) async throws -> AuthTokenResponse {
        let body = LoginRequest(email: email, password: password)
        return try await client.request(path: "auth/login", method: "POST", body: body)
    }

    func register(
        invitationToken: String,
        email: String,
        password: String,
        firstName: String,
        lastName: String,
        phone: String?
    ) async throws -> AuthTokenResponse {
        let body = RegisterResidentRequest(
            invitationToken: invitationToken,
            email: email,
            password: password,
            firstName: firstName,
            lastName: lastName,
            phone: phone
        )
        return try await client.request(path: "auth/register", method: "POST", body: body)
    }
}
