import SwiftUI

// MARK: - SosView

struct SosView: View {
    @EnvironmentObject private var tokenStore: TokenStore
    @EnvironmentObject private var router: NavigationRouter
    @StateObject private var viewModel = SosViewModel()

    @State private var showConfirmationDialog = false
    @State private var title = "Emergency"
    @State private var description = ""
    @State private var permissionToEnter = false
    @State private var formVisible = false

    var body: some View {
        Group {
            switch viewModel.uiState {
            case .idle where formVisible:
                formView
            case .idle:
                warningView
            case .loading:
                ProgressView("Alerting staff…")
                    .frame(maxWidth: .infinity, maxHeight: .infinity)
            case .success(let complaint):
                sosConfirmedView(complaint: complaint)
            case .error(let message):
                VStack(spacing: 16) {
                    Image(systemName: "exclamationmark.circle.fill")
                        .font(.largeTitle)
                        .foregroundStyle(.red)
                    Text(message)
                        .multilineTextAlignment(.center)
                    Button("Go Back") { router.pop() }
                        .buttonStyle(.bordered)
                }
                .padding()
            }
        }
        .navigationTitle("SOS Emergency")
        .navigationBarTitleDisplayMode(.inline)
        .confirmationDialog(
            "Confirm Emergency",
            isPresented: $showConfirmationDialog,
            titleVisibility: .visible
        ) {
            Button("CONFIRM EMERGENCY", role: .destructive) {
                formVisible = true
                showConfirmationDialog = false
            }
            Button("Cancel", role: .cancel) {}
        } message: {
            Text("This will immediately alert all on-call maintenance staff. Only use for genuine emergencies (fire, flooding, gas leak).")
        }
    }

    // MARK: - Warning / Launch View

    private var warningView: some View {
        VStack(spacing: 24) {
            Spacer()

            Image(systemName: "exclamationmark.triangle.fill")
                .font(.system(size: 72))
                .foregroundStyle(.red)

            Text("SOS Emergency Alert")
                .font(.title.bold())

            Text("This will immediately alert all on-call maintenance staff. Only use for genuine emergencies.")
                .multilineTextAlignment(.center)
                .foregroundStyle(.secondary)
                .padding(.horizontal)

            Button {
                showConfirmationDialog = true
            } label: {
                Label("CONFIRM EMERGENCY", systemImage: "exclamationmark.triangle.fill")
                    .frame(maxWidth: .infinity)
                    .padding(.vertical, 8)
            }
            .buttonStyle(.borderedProminent)
            .tint(.red)
            .padding(.horizontal)

            Button("Cancel") { router.pop() }
                .foregroundStyle(.secondary)

            Spacer()
        }
        .padding()
    }

    // MARK: - SOS Form

    private var formView: some View {
        Form {
            Section("Incident Details") {
                LabeledContent("Type") {
                    Text("Emergency")
                        .foregroundStyle(.secondary)
                }

                TextField("Title", text: $title)
                    .submitLabel(.next)

                VStack(alignment: .leading, spacing: 4) {
                    Text("Description")
                        .font(.subheadline)
                        .foregroundStyle(.secondary)
                    TextEditor(text: $description)
                        .frame(minHeight: 100)
                }
            }

            Section {
                Toggle("Permission to Enter Unit", isOn: $permissionToEnter)
            } footer: {
                Text("Allow maintenance staff to enter your unit if you are unavailable.")
            }

            Section {
                Button {
                    guard let token = tokenStore.accessToken else { return }
                    Task {
                        await viewModel.triggerSos(
                            title: title,
                            description: description,
                            permissionToEnter: permissionToEnter,
                            token: token
                        )
                    }
                } label: {
                    HStack {
                        Spacer()
                        Label("SEND EMERGENCY ALERT", systemImage: "exclamationmark.triangle.fill")
                            .bold()
                        Spacer()
                    }
                }
                .foregroundStyle(.white)
                .listRowBackground(Color.red)
                .disabled(title.trimmingCharacters(in: .whitespaces).isEmpty || description.trimmingCharacters(in: .whitespaces).isEmpty)
            }
        }
    }

    // MARK: - SOS Confirmed

    private func sosConfirmedView(complaint: ComplaintDTO) -> some View {
        VStack(spacing: 24) {
            Spacer()

            Image(systemName: "checkmark.shield.fill")
                .font(.system(size: 72))
                .foregroundStyle(.green)

            Text("Emergency Reported")
                .font(.title.bold())

            Text("All on-call maintenance staff have been alerted.")
                .multilineTextAlignment(.center)
                .foregroundStyle(.secondary)
                .padding(.horizontal)

            HStack {
                Text("Status:")
                    .foregroundStyle(.secondary)
                StatusBadge(status: complaint.status)
            }

            Text("Complaint #\(complaint.id)")
                .font(.caption)
                .foregroundStyle(.secondary)

            Button("View Complaint Details") {
                router.push(.complaintDetail(complaintId: complaint.id))
            }
            .buttonStyle(.borderedProminent)

            Button("Return Home") {
                router.popToRoot()
            }
            .foregroundStyle(.secondary)

            Spacer()
        }
        .padding()
    }
}
