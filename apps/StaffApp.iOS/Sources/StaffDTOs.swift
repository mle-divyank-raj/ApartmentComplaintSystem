import Foundation

// MARK: - StaffMemberDTO

struct StaffMemberDTO: Codable, Identifiable {
    let id: Int
    let fullName: String
    let email: String
    let phone: String?
    let state: StaffState
    let activeAssignments: [ComplaintDTO]
}

// MARK: - UpdateAvailabilityRequest

struct UpdateAvailabilityRequest: Encodable {
    let state: String
}
