import Foundation

// MARK: - Nested DTOs

struct MediaDTO: Codable, Identifiable {
    let mediaId: Int
    let url: String
    let type: String
    let uploadedAt: Date

    var id: Int { mediaId }
}

struct WorkNoteDTO: Codable, Identifiable {
    let workNoteId: Int
    let content: String
    let staffMemberId: Int
    let staffMemberName: String
    let createdAt: Date

    var id: Int { workNoteId }
}

struct StaffMemberSummaryDTO: Codable, Identifiable {
    let staffMemberId: Int
    let fullName: String
    let jobTitle: String?
    let availability: StaffState

    var id: Int { staffMemberId }
}

// MARK: - ComplaintDTO

struct ComplaintDTO: Codable, Identifiable {
    let complaintId: Int
    let propertyId: Int
    let unitId: Int
    let unitNumber: String?
    let buildingName: String?
    let residentId: Int
    let residentName: String?
    let title: String
    let description: String
    let category: String
    let urgency: Urgency
    let status: TicketStatus
    let permissionToEnter: Bool
    let assignedStaffMember: StaffMemberSummaryDTO?
    let media: [MediaDTO]
    let workNotes: [WorkNoteDTO]
    let eta: Date?
    let createdAt: Date
    let updatedAt: Date
    let resolvedAt: Date?
    let tat: Double?
    let residentRating: Int?
    let residentFeedbackComment: String?

    var id: Int { complaintId }
}

// MARK: - Paginated Response

struct ComplaintsPageDTO: Decodable {
    let items: [ComplaintDTO]
    let totalCount: Int
    let page: Int
    let pageSize: Int
}

// MARK: - Request DTOs

struct TriggerSosRequest: Encodable {
    let title: String
    let description: String
    let permissionToEnter: Bool
}

struct SubmitFeedbackRequest: Encodable {
    let rating: Int
    let comment: String?
}
