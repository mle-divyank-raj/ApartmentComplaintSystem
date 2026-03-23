import Foundation

// MARK: - ComplaintRepository

final class ComplaintRepository {
    private let client: APIClient

    init(client: APIClient = .shared) {
        self.client = client
    }

    func getMyComplaints(token: String, page: Int = 1) async throws -> ComplaintsPageDTO {
        return try await client.request(
            path: "complaints/my?page=\(page)&pageSize=20",
            token: token
        )
    }

    func getComplaint(id: Int, token: String) async throws -> ComplaintDTO {
        return try await client.request(path: "complaints/\(id)", token: token)
    }

    func submitComplaint(
        title: String,
        description: String,
        category: String,
        urgency: Urgency,
        permissionToEnter: Bool,
        mediaFiles: [MultipartFile],
        token: String
    ) async throws -> ComplaintDTO {
        let fields: [String: String] = [
            "title": title,
            "description": description,
            "category": category,
            "urgency": urgency.rawValue,
            "permissionToEnter": permissionToEnter ? "true" : "false"
        ]
        return try await client.requestMultipart(
            path: "complaints",
            fields: fields,
            fileFields: mediaFiles.isEmpty ? [:] : ["mediaFiles": mediaFiles],
            token: token
        )
    }

    func triggerSos(
        title: String,
        description: String,
        permissionToEnter: Bool,
        token: String
    ) async throws -> ComplaintDTO {
        let body = TriggerSosRequest(
            title: title,
            description: description,
            permissionToEnter: permissionToEnter
        )
        return try await client.request(
            path: "complaints/sos",
            method: "POST",
            body: body,
            token: token
        )
    }

    func submitFeedback(
        complaintId: Int,
        rating: Int,
        comment: String?,
        token: String
    ) async throws -> ComplaintDTO {
        let body = SubmitFeedbackRequest(rating: rating, comment: comment)
        return try await client.request(
            path: "complaints/\(complaintId)/feedback",
            method: "POST",
            body: body,
            token: token
        )
    }
}
