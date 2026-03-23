import SwiftUI

// MARK: - UpdateAvailabilityView

struct UpdateAvailabilityView: View {
    @EnvironmentObject private var tokenStore: TokenStore
    @EnvironmentObject private var router: NavigationRouter
    @StateObject private var viewModel = UpdateAvailabilityViewModel()

    // Staff can only manually set these 3 states; BUSY is set automatically
    private let manualStates: [StaffState] = [.available, .onBreak, .offDuty]
    @State private var selectedState: StaffState = .available

    var body: some View {
        switch viewModel.uiState {
        case .idle, .error:
            Form {
                Section {
                    ForEach(manualStates, id: \.self) { state in
                        HStack {
                            VStack(alignment: .leading, spacing: 2) {
                                Text(state.displayName)
                                    .font(.headline)
                                Text(subtitle(for: state))
                                    .font(.caption)
                                    .foregroundStyle(.secondary)
                            }
                            Spacer()
                            if selectedState == state {
                                Image(systemName: "checkmark")
                                    .foregroundStyle(.accentColor)
                            }
                        }
                        .contentShape(Rectangle())
                        .onTapGesture { selectedState = state }
                    }
                } header: {
                    Text("Select Availability")
                } footer: {
                    Text("\"Busy\" status is set automatically when you are assigned a complaint.")
                }

                if case .error(let message) = viewModel.uiState {
                    Section {
                        Text(message)
                            .foregroundStyle(.red)
                    }
                }

                Section {
                    Button {
                        guard let token = tokenStore.accessToken else { return }
                        Task {
                            await viewModel.updateAvailability(state: selectedState, token: token)
                        }
                    } label: {
                        Text("Update Availability")
                            .frame(maxWidth: .infinity, alignment: .center)
                    }
                }
            }
            .navigationTitle("Availability")
            .navigationBarTitleDisplayMode(.inline)

        case .loading:
            ProgressView("Updating…")
                .frame(maxWidth: .infinity, maxHeight: .infinity)
                .navigationTitle("Availability")

        case .success(let newState):
            VStack(spacing: 16) {
                Image(systemName: "checkmark.circle.fill")
                    .font(.largeTitle)
                    .foregroundStyle(.green)
                Text("Status updated to \(newState.displayName).")
                    .font(.headline)
                Button("Done") { router.pop() }
                    .buttonStyle(.borderedProminent)
            }
            .padding()
            .navigationTitle("Availability")
            .navigationBarBackButtonHidden()
        }
    }

    private func subtitle(for state: StaffState) -> String {
        switch state {
        case .available: return "Ready to receive assignments"
        case .onBreak: return "Temporarily unavailable"
        case .offDuty: return "Not working today"
        default: return ""
        }
    }
}
