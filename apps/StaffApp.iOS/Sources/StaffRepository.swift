import Foundation

// MARK: - StaffRepository

final class StaffRepository {
    private let client: APIClient

    init(client: APIClient = .shared) {
        self.client = client
    }

    func getMyProfile(token: String) async throws -> StaffMemberDTO {
        return try await client.request(path: "/staff/me", token: token)
    }

    func updateAvailability(state: StaffState, token: String) async throws -> StaffMemberDTO {
        let body = UpdateAvailabilityRequest(state: state.rawValue)
        return try await client.request(
            path: "/staff/me/availability",
            method: "PUT",
            body: body,
            token: token
        )
    }
}
