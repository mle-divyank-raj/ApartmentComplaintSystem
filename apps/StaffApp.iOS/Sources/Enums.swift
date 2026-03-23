import Foundation

// MARK: - TicketStatus

enum TicketStatus: String, Codable, CaseIterable {
    case open = "OPEN"
    case assigned = "ASSIGNED"
    case enRoute = "EN_ROUTE"
    case inProgress = "IN_PROGRESS"
    case resolved = "RESOLVED"
    case closed = "CLOSED"

    var displayName: String {
        switch self {
        case .open: return "Open"
        case .assigned: return "Assigned"
        case .enRoute: return "En Route"
        case .inProgress: return "In Progress"
        case .resolved: return "Resolved"
        case .closed: return "Closed"
        }
    }

    var timelineOrder: [TicketStatus] {
        [.open, .assigned, .enRoute, .inProgress, .resolved]
    }
}

// MARK: - Urgency

enum Urgency: String, Codable, CaseIterable {
    case low = "LOW"
    case medium = "MEDIUM"
    case high = "HIGH"
    case sosEmergency = "SOS_EMERGENCY"

    var displayName: String {
        switch self {
        case .low: return "Low"
        case .medium: return "Medium"
        case .high: return "High"
        case .sosEmergency: return "SOS Emergency"
        }
    }
}

// MARK: - StaffState

enum StaffState: String, Codable, CaseIterable {
    case available = "AVAILABLE"
    case busy = "BUSY"
    case onBreak = "ON_BREAK"
    case offDuty = "OFF_DUTY"

    var displayName: String {
        switch self {
        case .available: return "Available"
        case .busy: return "Busy"
        case .onBreak: return "On Break"
        case .offDuty: return "Off Duty"
        }
    }
}

// MARK: - OutageType

enum OutageType: String, Codable, CaseIterable {
    case electricity = "Electricity"
    case water = "Water"
    case gas = "Gas"
    case internet = "Internet"
    case elevator = "Elevator"
    case other = "Other"
}
