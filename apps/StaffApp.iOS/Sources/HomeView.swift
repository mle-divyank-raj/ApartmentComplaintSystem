import SwiftUI

// MARK: - HomeView (Staff)

struct HomeView: View {
    @EnvironmentObject private var tokenStore: TokenStore
    @EnvironmentObject private var router: NavigationRouter

    var body: some View {
        NavigationStack(path: Binding(
            get: { router.path },
            set: { router.path = $0 }
        )) {
            TaskListView()
                .toolbar {
                    ToolbarItem(placement: .navigationBarLeading) {
                        Button {
                            tokenStore.clear()
                        } label: {
                            Label("Sign Out", systemImage: "rectangle.portrait.and.arrow.right")
                        }
                    }
                }
                .navigationDestination(for: StaffAppRoute.self) { route in
                    destinationView(for: route)
                }
        }
    }

    @ViewBuilder
    private func destinationView(for route: StaffAppRoute) -> some View {
        switch route {
        case .login:
            LoginView()
        case .home:
            HomeView()
        case .taskDetail(let id):
            TaskDetailView(complaintId: id)
        case .updateEta(let id):
            UpdateEtaView(complaintId: id)
        case .addWorkNote(let id):
            AddWorkNoteView(complaintId: id)
        case .resolveComplaint(let id):
            ResolveComplaintView(complaintId: id)
        case .updateAvailability:
            UpdateAvailabilityView()
        }
    }
}
