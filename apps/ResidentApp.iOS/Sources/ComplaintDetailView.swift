import SwiftUI

// MARK: - ComplaintDetailView

struct ComplaintDetailView: View {
    @EnvironmentObject private var tokenStore: TokenStore
    @EnvironmentObject private var router: NavigationRouter
    @StateObject private var viewModel = ComplaintDetailViewModel()

    let complaintId: Int

    var body: some View {
        Group {
            switch viewModel.uiState {
            case .idle, .loading:
                ProgressView("Loading…")
                    .frame(maxWidth: .infinity, maxHeight: .infinity)

            case .success(let complaint):
                complaintContent(complaint)

            case .error(let message):
                VStack(spacing: 16) {
                    Image(systemName: "exclamationmark.triangle")
                        .font(.system(size: 48))
                        .foregroundStyle(.orange)
                    Text(message).multilineTextAlignment(.center)
                    Button("Retry") {
                        guard let token = tokenStore.accessToken else { return }
                        Task { await viewModel.loadComplaint(id: complaintId, token: token) }
                    }
                    .buttonStyle(.borderedProminent)
                }
                .padding(32)
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
    private func complaintContent(_ complaint: ComplaintDTO) -> some View {
        ScrollView {
            VStack(alignment: .leading, spacing: 20) {

                // Title + badges
                VStack(alignment: .leading, spacing: 8) {
                    Text(complaint.title)
                        .font(.title2.bold())
                    HStack {
                        StatusBadge(status: complaint.status)
                        UrgencyBadge(urgency: complaint.urgency)
                        Spacer()
                    }
                }

                // Status timeline
                StatusTimelineView(currentStatus: complaint.status)

                // Details section
                GroupBox("Details") {
                    VStack(alignment: .leading, spacing: 10) {
                        LabeledContent("Category", value: complaint.category)
                        LabeledContent("Permission to Enter", value: complaint.permissionToEnter ? "Yes" : "No")
                        if let unitNumber = complaint.unitNumber {
                            LabeledContent("Unit", value: unitNumber)
                        }
                        if let building = complaint.buildingName {
                            LabeledContent("Building", value: building)
                        }
                        if let staff = complaint.assignedStaffMember {
                            LabeledContent("Assigned To", value: staff.fullName)
                        }
                        if let eta = complaint.eta {
                            LabeledContent("Estimated Completion", value: eta.formatted(date: .abbreviated, time: .shortened))
                        }
                        if let tat = complaint.tat {
                            let hours = Int(tat) / 60
                            let minutes = Int(tat) % 60
                            LabeledContent("Resolved In", value: "\(hours)h \(minutes)m")
                        }
                    }
                    .font(.subheadline)
                }

                // Description
                GroupBox("Description") {
                    Text(complaint.description)
                        .font(.subheadline)
                        .frame(maxWidth: .infinity, alignment: .leading)
                }

                // Media thumbnails
                if !complaint.media.isEmpty {
                    GroupBox("Photos") {
                        ScrollView(.horizontal, showsIndicators: false) {
                            HStack(spacing: 10) {
                                ForEach(complaint.media) { media in
                                    AsyncImage(url: URL(string: media.url)) { image in
                                        image
                                            .resizable()
                                            .scaledToFill()
                                    } placeholder: {
                                        ProgressView()
                                    }
                                    .frame(width: 90, height: 90)
                                    .clipShape(RoundedRectangle(cornerRadius: 8))
                                }
                            }
                        }
                    }
                }

                // Work notes
                if !complaint.workNotes.isEmpty {
                    GroupBox("Work Notes") {
                        VStack(alignment: .leading, spacing: 12) {
                            ForEach(complaint.workNotes) { note in
                                VStack(alignment: .leading, spacing: 4) {
                                    HStack {
                                        Text(note.staffMemberName)
                                            .font(.caption.bold())
                                        Spacer()
                                        Text(note.createdAt.formatted(date: .abbreviated, time: .shortened))
                                            .font(.caption)
                                            .foregroundStyle(.secondary)
                                    }
                                    Text(note.content)
                                        .font(.subheadline)
                                }
                                Divider()
                            }
                        }
                    }
                }

                // Feedback prompt — shown when RESOLVED and no feedback yet
                if complaint.status == .resolved && complaint.residentRating == nil {
                    GroupBox {
                        VStack(spacing: 10) {
                            Text("How did we do?")
                                .font(.headline)
                            Text("Rate this repair and let us know.")
                                .font(.subheadline)
                                .foregroundStyle(.secondary)
                            Button("Leave Feedback") {
                                router.push(.feedback(complaintId: complaint.complaintId))
                            }
                            .buttonStyle(.borderedProminent)
                        }
                        .frame(maxWidth: .infinity)
                    }
                }

                // Rating display — shown when CLOSED
                if complaint.status == .closed, let rating = complaint.residentRating {
                    GroupBox("Your Feedback") {
                        VStack(alignment: .leading, spacing: 8) {
                            HStack(spacing: 4) {
                                ForEach(1...5, id: \.self) { star in
                                    Image(systemName: star <= rating ? "star.fill" : "star")
                                        .foregroundStyle(.yellow)
                                }
                            }
                            if let comment = complaint.residentFeedbackComment {
                                Text(comment)
                                    .font(.subheadline)
                            }
                        }
                    }
                }
            }
            .padding(16)
        }
    }
}

// MARK: - StatusTimelineView

struct StatusTimelineView: View {
    let currentStatus: TicketStatus

    private var steps: [TicketStatus] { TicketStatus.timelineOrder }

    var body: some View {
        HStack(spacing: 0) {
            ForEach(steps.indices, id: \.self) { idx in
                let step = steps[idx]
                let isCompleted = stepIndex(currentStatus) >= idx
                let isCurrent = currentStatus == step

                VStack(spacing: 4) {
                    Circle()
                        .fill(isCompleted ? Color.accentColor : Color(.systemGray4))
                        .frame(width: 12, height: 12)
                        .overlay {
                            if isCurrent {
                                Circle()
                                    .stroke(Color.accentColor, lineWidth: 2)
                                    .frame(width: 18, height: 18)
                            }
                        }
                    Text(step.displayName)
                        .font(.system(size: 9))
                        .foregroundStyle(isCompleted ? .primary : .secondary)
                        .multilineTextAlignment(.center)
                }
                .frame(maxWidth: .infinity)

                if idx < steps.count - 1 {
                    Rectangle()
                        .fill(stepIndex(currentStatus) > idx ? Color.accentColor : Color(.systemGray4))
                        .frame(height: 2)
                }
            }
        }
        .padding(.vertical, 8)
    }

    private func stepIndex(_ status: TicketStatus) -> Int {
        steps.firstIndex(of: status) ?? 0
    }
}
