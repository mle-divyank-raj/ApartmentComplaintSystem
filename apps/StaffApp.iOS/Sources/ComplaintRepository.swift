import Foundation

// MARK: - ComplaintRepository

final class ComplaintRepository {
    private let client: APIClient

    init(client: APIClient = .shared) {
        self.client = client
    }

    func getComplaint(id: Int, token: String) async throws -> ComplaintDTO {
        return try await client.request(path: "/complaints/\(id)", token: token)
    }

    func updateStatus(complaintId: Int, status: TicketStatus, token: String) async throws -> ComplaintDTO {
        let body = UpdateStatusRequest(status: status.rawValue)
        return try await client.request(
            path: "/complaints/\(complaintId)/status",
            method: "POST",
            body: body,
            token: token
        )
    }

    func updateEta(complaintId: Int, eta: Date, token: String) async throws -> ComplaintDTO {
        let body = UpdateEtaRequest(eta: eta)
        return try await client.request(
            path: "/complaints/\(complaintId)/eta",
            method: "POST",
            body: body,
            token: token
        )
    }

    func addWorkNote(complaintId: Int, note: String, token: String) async throws -> WorkNoteDTO {
        let body = AddWorkNoteRequest(note: note)
        return try await client.request(
            path: "/complaints/\(complaintId)/work-notes",
            method: "POST",
            body: body,
            token: token
        )
    }

    func resolveComplaint(
        complaintId: Int,
        resolutionNotes: String,
        completionPhotos: [MultipartFile],
        token: String
    ) async throws -> ComplaintDTO {
        return try await client.requestMultipart(
            path: "/complaints/\(complaintId)/resolve",
            method: "POST",
            fields: ["resolutionNotes": resolutionNotes],
            fileFields: completionPhotos,
            token: token
        )
    }
}
