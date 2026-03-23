import SwiftUI

// MARK: - ComplaintListView (My Complaints)

struct ComplaintListView: View {
    @EnvironmentObject private var tokenStore: TokenStore
    @EnvironmentObject private var router: NavigationRouter
    @StateObject private var viewModel = ComplaintListViewModel()

    var body: some View {
        Group {
            switch viewModel.uiState {
            case .idle, .loading:
                ProgressView("Loading complaints…")
                    .frame(maxWidth: .infinity, maxHeight: .infinity)

            case .success(let complaints):
                if complaints.isEmpty {
                    ContentUnavailableView(
                        "No Complaints",
                        systemImage: "tray",
                        description: Text("Tap '+' to submit your first complaint.")
                    )
                } else {
                    List(complaints) { complaint in
                        Button {
                            router.push(.complaintDetail(complaintId: complaint.complaintId))
                        } label: {
                            ComplaintRowView(complaint: complaint)
                        }
                        .buttonStyle(.plain)
                    }
                    .listStyle(.plain)
                }

            case .error(let message):
                VStack(spacing: 16) {
                    Image(systemName: "exclamationmark.triangle")
                        .font(.system(size: 48))
                        .foregroundStyle(.orange)
                    Text(message)
                        .multilineTextAlignment(.center)
                    Button("Retry") {
                        guard let token = tokenStore.accessToken else { return }
                        Task { await viewModel.loadComplaints(token: token) }
                    }
                    .buttonStyle(.borderedProminent)
                }
                .padding(32)
            }
        }
        .navigationTitle("My Complaints")
        .task {
            guard let token = tokenStore.accessToken else { return }
            await viewModel.loadComplaints(token: token)
        }
    }
}

// MARK: - ComplaintRowView

struct ComplaintRowView: View {
    let complaint: ComplaintDTO

    var body: some View {
        VStack(alignment: .leading, spacing: 8) {
            HStack {
                Text(complaint.title)
                    .font(.headline)
                    .lineLimit(2)
                Spacer()
                UrgencyBadge(urgency: complaint.urgency)
            }
            HStack(spacing: 8) {
                StatusBadge(status: complaint.status)
                Text(complaint.createdAt.formatted(date: .abbreviated, time: .omitted))
                    .font(.caption)
                    .foregroundStyle(.secondary)
            }
        }
        .padding(.vertical, 4)
    }
}

// MARK: - UrgencyBadge

struct UrgencyBadge: View {
    let urgency: Urgency

    private var color: Color {
        switch urgency {
        case .low: return .green
        case .medium: return .yellow
        case .high: return .orange
        case .sosEmergency: return .red
        }
    }

    var body: some View {
        Text(urgency.displayName)
            .font(.caption.bold())
            .padding(.horizontal, 8)
            .padding(.vertical, 3)
            .background(color.opacity(0.2))
            .foregroundStyle(color)
            .clipShape(Capsule())
    }
}

// MARK: - StatusBadge

struct StatusBadge: View {
    let status: TicketStatus

    private var color: Color {
        switch status {
        case .open: return .blue
        case .assigned: return .purple
        case .enRoute: return .indigo
        case .inProgress: return .orange
        case .resolved: return .green
        case .closed: return .gray
        }
    }

    var body: some View {
        Text(status.displayName)
            .font(.caption.bold())
            .padding(.horizontal, 8)
            .padding(.vertical, 3)
            .background(color.opacity(0.2))
            .foregroundStyle(color)
            .clipShape(Capsule())
    }
}
