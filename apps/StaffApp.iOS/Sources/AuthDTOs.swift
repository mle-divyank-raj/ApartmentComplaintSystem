import Foundation

// MARK: - Auth DTOs

struct LoginRequest: Encodable {
    let email: String
    let password: String
}

struct AuthTokenResponse: Decodable {
    let accessToken: String
    let userId: Int?
    let role: String?
    let staffMemberId: Int?
}
