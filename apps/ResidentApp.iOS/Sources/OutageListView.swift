import SwiftUI

// MARK: - OutageListView

struct OutageListView: View {
    @EnvironmentObject private var tokenStore: TokenStore
    @StateObject private var viewModel = OutageListViewModel()

    private static let dateFormatter: DateFormatter = {
        let f = DateFormatter()
        f.dateStyle = .medium
        f.timeStyle = .short
        return f
    }()

    var body: some View {
        Group {
            switch viewModel.uiState {
            case .idle, .loading:
                ProgressView()
                    .frame(maxWidth: .infinity, maxHeight: .infinity)
            case .empty:
                ContentUnavailableView(
                    "No Active Outages",
                    systemImage: "bolt.slash",
                    description: Text("There are no reported outages in your property.")
                )
            case .success(let outages):
                List(outages) { outage in
                    OutageRowView(outage: outage)
                }
                .listStyle(.plain)
            case .error(let message):
                VStack(spacing: 16) {
                    Image(systemName: "exclamationmark.circle")
                        .font(.largeTitle)
                        .foregroundStyle(.secondary)
                    Text(message)
                        .multilineTextAlignment(.center)
                    Button("Retry") {
                        guard let token = tokenStore.accessToken else { return }
                        Task { await viewModel.loadOutages(token: token) }
                    }
                    .buttonStyle(.bordered)
                }
                .padding()
            }
        }
        .navigationTitle("Outages")
        .navigationBarTitleDisplayMode(.inline)
        .task {
            guard let token = tokenStore.accessToken else { return }
            await viewModel.loadOutages(token: token)
        }
        .refreshable {
            guard let token = tokenStore.accessToken else { return }
            await viewModel.loadOutages(token: token)
        }
    }
}

// MARK: - OutageRowView

struct OutageRowView: View {
    let outage: OutageDTO

    private static let dateFormatter: DateFormatter = {
        let f = DateFormatter()
        f.dateStyle = .medium
        f.timeStyle = .short
        return f
    }()

    var body: some View {
        VStack(alignment: .leading, spacing: 6) {
            HStack {
                OutageTypeBadge(outageType: outage.outageType)
                Spacer()
                if outage.resolvedAt == nil {
                    Text("Ongoing")
                        .font(.caption.bold())
                        .foregroundStyle(.white)
                        .padding(.horizontal, 8)
                        .padding(.vertical, 3)
                        .background(.red, in: Capsule())
                } else {
                    Text("Resolved")
                        .font(.caption.bold())
                        .foregroundStyle(.white)
                        .padding(.horizontal, 8)
                        .padding(.vertical, 3)
                        .background(.green, in: Capsule())
                }
            }

            Text(outage.title)
                .font(.headline)

            if let description = outage.description {
                Text(description)
                    .font(.subheadline)
                    .foregroundStyle(.secondary)
                    .lineLimit(2)
            }

            HStack {
                Label(OutageRowView.dateFormatter.string(from: outage.startTime), systemImage: "clock")
                    .font(.caption)
                    .foregroundStyle(.secondary)

                if let resolvedAt = outage.resolvedAt {
                    Text("→")
                        .font(.caption)
                        .foregroundStyle(.secondary)
                    Text(OutageRowView.dateFormatter.string(from: resolvedAt))
                        .font(.caption)
                        .foregroundStyle(.secondary)
                }
            }
        }
        .padding(.vertical, 4)
    }
}

// MARK: - OutageTypeBadge

struct OutageTypeBadge: View {
    let outageType: OutageType

    var body: some View {
        Text(outageType.displayName)
            .font(.caption.bold())
            .foregroundStyle(.white)
            .padding(.horizontal, 8)
            .padding(.vertical, 3)
            .background(outageType.badgeColor, in: Capsule())
    }
}

private extension OutageType {
    var displayName: String {
        switch self {
        case .electricity: return "Electricity"
        case .water: return "Water"
        case .gas: return "Gas"
        case .internet: return "Internet"
        case .elevator: return "Elevator"
        case .other: return "Other"
        }
    }

    var badgeColor: Color {
        switch self {
        case .electricity: return .orange
        case .water: return .blue
        case .gas: return .purple
        case .internet: return .teal
        case .elevator: return .brown
        case .other: return .gray
        }
    }
}
