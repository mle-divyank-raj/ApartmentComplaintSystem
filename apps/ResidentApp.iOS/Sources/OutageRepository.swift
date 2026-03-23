import Foundation

// MARK: - OutageRepository

final class OutageRepository {
    private let client: APIClient

    init(client: APIClient = .shared) {
        self.client = client
    }

    func getOutages(token: String) async throws -> [OutageDTO] {
        return try await client.request(path: "outages", token: token)
    }

    func getOutage(id: Int, token: String) async throws -> OutageDTO {
        return try await client.request(path: "outages/\(id)", token: token)
    }
}
