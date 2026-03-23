import Foundation

// MARK: - AuthRepository

final class AuthRepository {
    private let client: APIClient

    init(client: APIClient = .shared) {
        self.client = client
    }

    func login(email: String, password: String) async throws -> AuthTokenResponse {
        let body = LoginRequest(email: email, password: password)
        return try await client.request(path: "/auth/login", method: "POST", body: body)
    }
}
