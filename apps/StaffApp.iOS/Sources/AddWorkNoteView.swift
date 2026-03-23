import SwiftUI

// MARK: - AddWorkNoteView

struct AddWorkNoteView: View {
    let complaintId: Int

    @EnvironmentObject private var tokenStore: TokenStore
    @EnvironmentObject private var router: NavigationRouter
    @StateObject private var viewModel = AddWorkNoteViewModel()

    @State private var note = ""

    var body: some View {
        switch viewModel.uiState {
        case .idle, .error:
            Form {
                Section("Work Note") {
                    TextEditor(text: $note)
                        .frame(minHeight: 120)
                    Text("\(note.count) characters")
                        .font(.caption)
                        .foregroundStyle(.secondary)
                        .frame(maxWidth: .infinity, alignment: .trailing)
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
                            await viewModel.addWorkNote(
                                complaintId: complaintId,
                                note: note,
                                token: token
                            )
                        }
                    } label: {
                        Text("Add Note")
                            .frame(maxWidth: .infinity, alignment: .center)
                    }
                    .disabled(note.trimmingCharacters(in: .whitespacesAndNewlines).isEmpty)
                }
            }
            .navigationTitle("Add Work Note")
            .navigationBarTitleDisplayMode(.inline)

        case .loading:
            ProgressView("Saving…")
                .frame(maxWidth: .infinity, maxHeight: .infinity)
                .navigationTitle("Add Work Note")

        case .success:
            VStack(spacing: 16) {
                Image(systemName: "checkmark.circle.fill")
                    .font(.largeTitle)
                    .foregroundStyle(.green)
                Text("Work note added.")
                Button("Done") { router.pop() }
                    .buttonStyle(.borderedProminent)
            }
            .padding()
            .navigationTitle("Add Work Note")
            .navigationBarBackButtonHidden()
        }
    }
}
