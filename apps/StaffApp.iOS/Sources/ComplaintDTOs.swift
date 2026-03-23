import Foundation

// MARK: - MediaDTO

struct MediaDTO: Codable, Identifiable {
    let id: Int
    let url: String
    let mediaType: String
}

// MARK: - WorkNoteDTO

struct WorkNoteDTO: Codable, Identifiable {
    let id: Int
    let note: String
    let createdAt: Date
    let authorName: String
}

// MARK: - StaffMemberSummaryDTO

struct StaffMemberSummaryDTO: Codable {
    let id: Int
    let fullName: String
    let phone: String?
}

// MARK: - ComplaintDTO

struct ComplaintDTO: Codable, Identifiable {
    let id: Int
    let title: String
    let description: String
    let category: String
    let urgency: Urgency
    let status: TicketStatus
    let permissionToEnter: Bool
    let unitNumber: String?
    let buildingName: String?
    let assignedStaff: StaffMemberSummaryDTO?
    let media: [MediaDTO]
    let workNotes: [WorkNoteDTO]
    let eta: Date?
    let tat: Double?
    let residentRating: Int?
    let residentFeedbackComment: String?
    let createdAt: Date
    let updatedAt: Date
}

// MARK: - UpdateStatusRequest

struct UpdateStatusRequest: Encodable {
    let status: String
}

// MARK: - UpdateEtaRequest

struct UpdateEtaRequest: Encodable {
    let eta: Date

    func encode(to encoder: Encoder) throws {
        var container = encoder.container(keyedBy: CodingKeys.self)
        let formatter = ISO8601DateFormatter()
        try container.encode(formatter.string(from: eta), forKey: .eta)
    }

    private enum CodingKeys: String, CodingKey {
        case eta
    }
}

// MARK: - AddWorkNoteRequest

struct AddWorkNoteRequest: Encodable {
    let note: String
}
