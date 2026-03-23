import SwiftUI

// MARK: - HomeView

struct HomeView: View {
    @EnvironmentObject private var tokenStore: TokenStore
    @EnvironmentObject private var router: NavigationRouter

    var body: some View {
        NavigationStack(path: Binding(
            get: { router.path },
            set: { router.path = $0 }
        )) {
            homeContent
                .navigationTitle("ACLS Resident")
                .navigationBarTitleDisplayMode(.large)
                .toolbar {
                    ToolbarItem(placement: .navigationBarTrailing) {
                        Button {
                            tokenStore.clear()
                        } label: {
                            Label("Sign Out", systemImage: "rectangle.portrait.and.arrow.right")
                        }
                    }
                }
                .navigationDestination(for: AppRoute.self) { route in
                    destinationView(for: route)
                }
        }
    }

    private var homeContent: some View {
        VStack(spacing: 0) {
            // Greeting
            HStack {
                VStack(alignment: .leading, spacing: 2) {
                    Text("Welcome back")
                        .font(.subheadline)
                        .foregroundStyle(.secondary)
                    Text("Unit Resident")
                        .font(.title2.bold())
                }
                Spacer()
            }
            .padding()

            Divider()

            // Main nav buttons
            VStack(spacing: 16) {
                NavCard(
                    title: "My Complaints",
                    subtitle: "Track existing complaints",
                    icon: "list.bullet.rectangle",
                    color: .blue
                ) {
                    router.push(.complaintList)
                }

                NavCard(
                    title: "Submit Complaint",
                    subtitle: "Report a new issue",
                    icon: "plus.circle",
                    color: .indigo
                ) {
                    router.push(.submitComplaint)
                }

                NavCard(
                    title: "Outages",
                    subtitle: "View property outages",
                    icon: "bolt.slash",
                    color: .orange
                ) {
                    router.push(.outageList)
                }
            }
            .padding()

            Spacer()

            // Prominent SOS button at bottom
            Button {
                router.push(.sos)
            } label: {
                HStack(spacing: 12) {
                    Image(systemName: "exclamationmark.triangle.fill")
                        .font(.title2)
                    VStack(alignment: .leading, spacing: 2) {
                        Text("SOS EMERGENCY")
                            .font(.headline.bold())
                        Text("Fire, flooding, gas leak")
                            .font(.caption)
                    }
                    Spacer()
                    Image(systemName: "chevron.right")
                        .foregroundStyle(.white.opacity(0.7))
                }
                .foregroundStyle(.white)
                .padding()
                .background(.red, in: RoundedRectangle(cornerRadius: 16))
                .padding()
            }
        }
    }

    @ViewBuilder
    private func destinationView(for route: AppRoute) -> some View {
        switch route {
        case .login:
            LoginView()
        case .register(let token):
            RegisterView(invitationToken: token)
        case .home:
            HomeView()
        case .complaintList:
            ComplaintListView()
        case .complaintDetail(let id):
            ComplaintDetailView(complaintId: id)
        case .submitComplaint:
            SubmitComplaintView()
        case .submitComplaintConfirmation(let id, let title):
            SubmitComplaintConfirmationView(complaintId: id, complaintTitle: title)
        case .sos:
            SosView()
        case .sosConfirmed:
            EmptyView()
        case .feedback(let id):
            FeedbackView(complaintId: id)
        case .feedbackThankYou:
            ThankYouView()
        case .outageList:
            OutageListView()
        }
    }
}

// MARK: - NavCard

private struct NavCard: View {
    let title: String
    let subtitle: String
    let icon: String
    let color: Color
    let action: () -> Void

    var body: some View {
        Button(action: action) {
            HStack(spacing: 16) {
                Image(systemName: icon)
                    .font(.title2)
                    .foregroundStyle(.white)
                    .frame(width: 48, height: 48)
                    .background(color, in: RoundedRectangle(cornerRadius: 12))

                VStack(alignment: .leading, spacing: 2) {
                    Text(title)
                        .font(.headline)
                        .foregroundStyle(.primary)
                    Text(subtitle)
                        .font(.subheadline)
                        .foregroundStyle(.secondary)
                }
                Spacer()
                Image(systemName: "chevron.right")
                    .foregroundStyle(.secondary)
            }
            .padding()
            .background(.regularMaterial, in: RoundedRectangle(cornerRadius: 16))
        }
        .buttonStyle(.plain)
    }
}
