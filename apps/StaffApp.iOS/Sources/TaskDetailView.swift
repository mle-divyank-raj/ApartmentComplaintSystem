import SwiftUI

// MARK: - TaskDetailView

struct TaskDetailView: View {
    let complaintId: Int

    @EnvironmentObject private var tokenStore: TokenStore
    @EnvironmentObject private var router: NavigationRouter
    @StateObject private var viewModel = TaskDetailViewModel()

    var body: some View {
        Group {
            switch viewModel.uiState {
            case .idle, .loading:
                ProgressView()
                    .frame(maxWidth: .infinity, maxHeight: .infinity)
            case .success(let complaint):
                complaintDetail(complaint: complaint)
            case .error(let message):
                VStack(spacing: 16) {
                    Image(systemName: "exclamationmark.circle")
                        .font(.largeTitle)
                        .foregroundStyle(.secondary)
                    Text(message)
                        .multilineTextAlignment(.center)
                    Button("Retry") {
                        guard let token = tokenStore.accessToken else { return }
                        Task { await viewModel.loadComplaint(id: complaintId, token: token) }
                    }
                    .buttonStyle(.bordered)
                }
                .padding()
            }
        }
        .navigationTitle("Complaint #\(complaintId)")
        .navigationBarTitleDisplayMode(.inline)
        .task {
            guard let token = tokenStore.accessToken else { return }
            await viewModel.loadComplaint(id: complaintId, token: token)
        }
    }

    @ViewBuilder
    private func complaintDetail(complaint: ComplaintDTO) -> some View {
        ScrollView {
            VStack(alignment: .leading, spacing: 16) {
                // Header
                HStack {
                    UrgencyBadge(urgency: complaint.urgency)
                    StatusBadge(status: complaint.status)
                    Spacer()
                }
                .padding(.horizontal)

                Text(complaint.title)
                    .font(.title2.bold())
                    .padding(.horizontal)

                Text(complaint.description)
                    .foregroundStyle(.secondary)
                    .padding(.horizontal)

                // Details
                GroupBox("Details") {
                    VStack(alignment: .leading, spacing: 8) {
                        LabeledContent("Category", value: complaint.category)
                        LabeledContent("Permission to Enter", value: complaint.permissionToEnter ? "Yes" : "No")
                        if let unit = complaint.unitNumber {
                            LabeledContent("Unit", value: unit)
                        }
                        if let building = complaint.buildingName {
                            LabeledContent("Building", value: building)
                        }
                        if let eta = complaint.eta {
                            LabeledContent("ETA") {
                                Text(eta, style: .datetime)
                            }
                        }
                        if let tat = complaint.tat {
                            let totalMins = Int(tat)
                            LabeledContent("Resolution Time", value: "\(totalMins / 60)h \(totalMins % 60)m")
                        }
                    }
                }
                .padding(.horizontal)

                // Photos
                if !complaint.media.isEmpty {
                    GroupBox("Attached Photos") {
                        ScrollView(.horizontal, showsIndicators: false) {
                            HStack(spacing: 8) {
                                ForEach(complaint.media) { media in
                                    AsyncImage(url: URL(string: media.url)) { image in
                                        image.resizable().scaledToFill()
                                    } placeholder: {
                                        Color.secondary.opacity(0.2)
                                    }
                                    .frame(width: 100, height: 100)
                                    .clipShape(RoundedRectangle(cornerRadius: 8))
                                }
                            }
                        }
                    }
                    .padding(.horizontal)
                }

                // Work Notes
                if !complaint.workNotes.isEmpty {
                    GroupBox("Work Notes") {
                        VStack(alignment: .leading, spacing: 12) {
                            ForEach(complaint.workNotes.sorted(by: { $0.createdAt < $1.createdAt })) { note in
                                VStack(alignment: .leading, spacing: 4) {
                                    Text(note.note)
                                        .font(.subheadline)
                                    HStack {
                                        Text(note.authorName)
                                            .font(.caption.bold())
                                            .foregroundStyle(.secondary)
                                        Spacer()
                                        Text(note.createdAt, style: .relative)
                                            .font(.caption)
                                            .foregroundStyle(.secondary)
                                    }
                                }
                                if note.id != complaint.workNotes.last?.id {
                                    Divider()
                                }
                            }
                        }
                    }
                    .padding(.horizontal)
                }

                // Error message
                if let error = viewModel.statusErrorMessage {
                    Text(error)
                        .font(.caption)
                        .foregroundStyle(.red)
                        .padding(.horizontal)
                }

                // Context-sensitive action buttons
                actionButtons(complaint: complaint)
                    .padding()

                Spacer(minLength: 32)
            }
            .padding(.vertical)
        }
    }

    @ViewBuilder
    private func actionButtons(complaint: ComplaintDTO) -> some View {
        VStack(spacing: 12) {
            switch complaint.status {
            case .assigned:
                Button {
                    guard let token = tokenStore.accessToken else { return }
                    Task {
                        await viewModel.updateStatus(
                            complaintId: complaint.id,
                            newStatus: .enRoute,
                            token: token
                        )
                    }
                } label: {
                    Label("Accept & En Route", systemImage: "car.fill")
                        .frame(maxWidth: .infinity)
                        .padding(.vertical, 4)
                }
                .buttonStyle(.borderedProminent)
                .disabled(viewModel.isUpdatingStatus)

            case .enRoute:
                Button {
                    guard let token = tokenStore.accessToken else { return }
                    Task {
                        await viewModel.updateStatus(
                            complaintId: complaint.id,
                            newStatus: .inProgress,
                            token: token
                        )
                    }
                } label: {
                    Label("Start Work", systemImage: "wrench.fill")
                        .frame(maxWidth: .infinity)
                        .padding(.vertical, 4)
                }
                .buttonStyle(.borderedProminent)
                .tint(.orange)
                .disabled(viewModel.isUpdatingStatus)

                Button {
                    router.push(.updateEta(complaintId: complaint.id))
                } label: {
                    Label("Update ETA", systemImage: "clock")
                        .frame(maxWidth: .infinity)
                        .padding(.vertical, 4)
                }
                .buttonStyle(.bordered)

            case .inProgress:
                Button {
                    router.push(.resolveComplaint(complaintId: complaint.id))
                } label: {
                    Label("Mark Resolved", systemImage: "checkmark.circle.fill")
                        .frame(maxWidth: .infinity)
                        .padding(.vertical, 4)
                }
                .buttonStyle(.borderedProminent)
                .tint(.green)

                Button {
                    router.push(.addWorkNote(complaintId: complaint.id))
                } label: {
                    Label("Add Work Note", systemImage: "note.text.badge.plus")
                        .frame(maxWidth: .infinity)
                        .padding(.vertical, 4)
                }
                .buttonStyle(.bordered)

                Button {
                    router.push(.updateEta(complaintId: complaint.id))
                } label: {
                    Label("Update ETA", systemImage: "clock")
                        .frame(maxWidth: .infinity)
                        .padding(.vertical, 4)
                }
                .buttonStyle(.bordered)

            default:
                EmptyView()
            }
        }
    }
}
