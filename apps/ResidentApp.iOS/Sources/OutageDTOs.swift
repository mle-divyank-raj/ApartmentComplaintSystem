import Foundation

struct OutageDTO: Codable, Identifiable {
    let outageId: Int
    let title: String
    let outageType: OutageType
    let description: String
    let startTime: Date
    let endTime: Date
    let declaredAt: Date
    let notificationSentAt: Date?

    var id: Int { outageId }
}
