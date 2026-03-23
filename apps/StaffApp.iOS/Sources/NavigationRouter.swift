import SwiftUI

// MARK: - StaffAppRoute

enum StaffAppRoute: Hashable {
    case login
    case home
    case taskDetail(complaintId: Int)
    case updateEta(complaintId: Int)
    case addWorkNote(complaintId: Int)
    case resolveComplaint(complaintId: Int)
    case updateAvailability
}

// MARK: - NavigationRouter

@MainActor
final class NavigationRouter: ObservableObject {
    @Published var path = NavigationPath()

    func push(_ route: StaffAppRoute) {
        path.append(route)
    }

    func pop() {
        guard !path.isEmpty else { return }
        path.removeLast()
    }

    func popToRoot() {
        path = NavigationPath()
    }
}
