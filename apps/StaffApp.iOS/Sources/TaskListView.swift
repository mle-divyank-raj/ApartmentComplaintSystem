import SwiftUI

// MARK: - TaskListView

struct TaskListView: View {
    @EnvironmentObject private var tokenStore: TokenStore
    @EnvironmentObject private var router: NavigationRouter
    @StateObject private var viewModel = TaskListViewModel()

    var body: some View {
        Group {
            switch viewModel.uiState {
            case .idle, .loading:
                ProgressView()
                    .frame(maxWidth: .infinity, maxHeight: .infinity)
            case .success(let profile):
                taskList(profile: profile)
            case .error(let message):
                VStack(spacing: 16) {
                    Image(systemName: "exclamationmark.circle")
                        .font(.largeTitle)
                        .foregroundStyle(.secondary)
                    Text(message)
                        .multilineTextAlignment(.center)
                    Button("Retry") {
                        guard let token = tokenStore.accessToken else { return }
                        Task { await viewModel.loadProfile(token: token) }
                    }
                    .buttonStyle(.bordered)
                }
                .padding()
            }
        }
        .navigationTitle("My Tasks")
        .navigationBarTitleDisplayMode(.large)
        .toolbar {
            ToolbarItem(placement: .navigationBarTrailing) {
                Button {
                    router.push(.updateAvailability)
                } label: {
                    Label("Availability", systemImage: "person.crop.circle.badge.checkmark")
                }
            }
        }
        .task {
            guard let token = tokenStore.accessToken else { return }
            await viewModel.loadProfile(token: token)
        }
        .refreshable {
            guard let token = tokenStore.accessToken else { return }
            await viewModel.loadProfile(token: token)
        }
    }

    private func taskList(profile: StaffMemberDTO) -> some View {
        List {
            Section {
                HStack {
                    VStack(alignment: .leading, spacing: 2) {
                        Text(profile.fullName)
                            .font(.headline)
                        Text(profile.email)
                            .font(.subheadline)
                            .foregroundStyle(.secondary)
                    }
                    Spacer()
                    StaffStateBadge(state: profile.state)
                }
                .padding(.vertical, 4)
            } header: {
                Text("Profile")
            }

            Section {
                if profile.activeAssignments.isEmpty {
                    ContentUnavailableView(
                        "No Active Tasks",
                        systemImage: "checkmark.circle",
                        description: Text("You have no active assignments.")
                    )
                    .listRowBackground(Color.clear)
                } else {
                    ForEach(profile.activeAssignments) { complaint in
                        Button {
                            router.push(.taskDetail(complaintId: complaint.id))
                        } label: {
                            TaskRowView(complaint: complaint)
                        }
                        .buttonStyle(.plain)
                    }
                }
            } header: {
                Text("Active Assignments (\(profile.activeAssignments.count))")
            }
        }
        .listStyle(.insetGrouped)
    }
}

// MARK: - TaskRowView

struct TaskRowView: View {
    let complaint: ComplaintDTO

    var body: some View {
        VStack(alignment: .leading, spacing: 6) {
            HStack {
                UrgencyBadge(urgency: complaint.urgency)
                StatusBadge(status: complaint.status)
                Spacer()
                Text("#\(complaint.id)")
                    .font(.caption)
                    .foregroundStyle(.secondary)
            }
            Text(complaint.title)
                .font(.headline)
                .lineLimit(2)
            if let unit = complaint.unitNumber, let building = complaint.buildingName {
                Label("\(building) – Unit \(unit)", systemImage: "building.2")
                    .font(.caption)
                    .foregroundStyle(.secondary)
            }
        }
        .padding(.vertical, 4)
    }
}

// MARK: - StaffStateBadge

struct StaffStateBadge: View {
    let state: StaffState

    var body: some View {
        Text(state.displayName)
            .font(.caption.bold())
            .foregroundStyle(.white)
            .padding(.horizontal, 10)
            .padding(.vertical, 4)
            .background(badgeColor, in: Capsule())
    }

    private var badgeColor: Color {
        switch state {
        case .available: return .green
        case .busy: return .orange
        case .onBreak: return .yellow
        case .offDuty: return .gray
        }
    }
}

// MARK: - UrgencyBadge (shared)

struct UrgencyBadge: View {
    let urgency: Urgency

    var body: some View {
        Text(urgency.displayName)
            .font(.caption.bold())
            .foregroundStyle(.white)
            .padding(.horizontal, 8)
            .padding(.vertical, 3)
            .background(badgeColor, in: Capsule())
    }

    private var badgeColor: Color {
        switch urgency {
        case .low: return .green
        case .medium: return .yellow
        case .high: return .orange
        case .sosEmergency: return .red
        }
    }
}

// MARK: - StatusBadge (shared)

struct StatusBadge: View {
    let status: TicketStatus

    var body: some View {
        Text(status.displayName)
            .font(.caption.bold())
            .foregroundStyle(.white)
            .padding(.horizontal, 8)
            .padding(.vertical, 3)
            .background(badgeColor, in: Capsule())
    }

    private var badgeColor: Color {
        switch status {
        case .open: return .blue
        case .assigned: return .purple
        case .enRoute: return .teal
        case .inProgress: return .orange
        case .resolved: return .green
        case .closed: return .gray
        }
    }
}
