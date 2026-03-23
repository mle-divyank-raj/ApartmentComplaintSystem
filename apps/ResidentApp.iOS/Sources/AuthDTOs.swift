import Foundation

// MARK: - Auth Request DTOs

struct LoginRequest: Encodable {
    let email: String
    let password: String
}

struct RegisterResidentRequest: Encodable {
    let invitationToken: String
    let email: String
    let password: String
    let firstName: String
    let lastName: String
    let phone: String?
}

// MARK: - Auth Response DTOs

struct AuthTokenResponse: Decodable {
    let accessToken: String
    let expiresAt: Date
    let userId: Int
    let role: String
    let propertyId: Int
}
