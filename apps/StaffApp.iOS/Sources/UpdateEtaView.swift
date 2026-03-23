import SwiftUI

// MARK: - UpdateEtaView

struct UpdateEtaView: View {
    let complaintId: Int

    @EnvironmentObject private var tokenStore: TokenStore
    @EnvironmentObject private var router: NavigationRouter
    @StateObject private var viewModel = UpdateEtaViewModel()

    @State private var eta = Date().addingTimeInterval(3600) // default 1h from now

    var body: some View {
        switch viewModel.uiState {
        case .idle, .error:
            Form {
                Section("Estimated Arrival Time") {
                    DatePicker(
                        "ETA",
                        selection: $eta,
                        in: Date()...,
                        displayedComponents: [.date, .hourAndMinute]
                    )
                    .datePickerStyle(.graphical)
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
                            await viewModel.updateEta(
                                complaintId: complaintId,
                                eta: eta,
                                token: token
                            )
                        }
                    } label: {
                        Text("Set ETA")
                            .frame(maxWidth: .infinity, alignment: .center)
                    }
                }
            }
            .navigationTitle("Update ETA")
            .navigationBarTitleDisplayMode(.inline)

        case .loading:
            ProgressView("Saving…")
                .frame(maxWidth: .infinity, maxHeight: .infinity)
                .navigationTitle("Update ETA")

        case .success:
            VStack(spacing: 16) {
                Image(systemName: "checkmark.circle.fill")
                    .font(.largeTitle)
                    .foregroundStyle(.green)
                Text("ETA updated successfully.")
                Button("Done") { router.pop() }
                    .buttonStyle(.borderedProminent)
            }
            .padding()
            .navigationTitle("Update ETA")
            .navigationBarBackButtonHidden()
        }
    }
}
