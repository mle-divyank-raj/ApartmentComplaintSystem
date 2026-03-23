import SwiftUI

// MARK: - AppRoute

enum AppRoute: Hashable {
    case login
    case register(invitationToken: String)
    case home
    case complaintList
    case complaintDetail(complaintId: Int)
    case submitComplaint
    case submitComplaintConfirmation(complaintId: Int, title: String)
    case sos
    case sosConfirmed
    case feedback(complaintId: Int)
    case feedbackThankYou
    case outageList
}

// MARK: - NavigationRouter

@MainActor
final class NavigationRouter: ObservableObject {
    @Published var path = NavigationPath()

    func push(_ route: AppRoute) {
        path.append(route)
    }

    func pop() {
        path.removeLast()
    }

    func popToRoot() {
        path.removeLast(path.count)
    }
}
