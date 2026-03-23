import SwiftUI
import PhotosUI

// MARK: - ResolveComplaintView

struct ResolveComplaintView: View {
    let complaintId: Int

    @EnvironmentObject private var tokenStore: TokenStore
    @EnvironmentObject private var router: NavigationRouter
    @StateObject private var viewModel = ResolveComplaintViewModel()

    @State private var resolutionNotes = ""

    var body: some View {
        switch viewModel.uiState {
        case .idle, .error:
            Form {
                Section("Resolution Notes (required)") {
                    TextEditor(text: $resolutionNotes)
                        .frame(minHeight: 120)
                }

                Section {
                    PhotosPicker(
                        selection: $viewModel.selectedPhotos,
                        maxSelectionCount: 3,
                        matching: .images
                    ) {
                        Label("Attach Completion Photos (max 3)", systemImage: "photo.on.rectangle.angled")
                    }
                    .onChange(of: viewModel.selectedPhotos) { _ in
                        Task { await viewModel.loadSelectedPhotos() }
                    }

                    if !viewModel.photoDataList.isEmpty {
                        ScrollView(.horizontal, showsIndicators: false) {
                            HStack(spacing: 8) {
                                ForEach(Array(viewModel.photoDataList.enumerated()), id: \.offset) { _, data in
                                    if let image = UIImage(data: data) {
                                        Image(uiImage: image)
                                            .resizable()
                                            .scaledToFill()
                                            .frame(width: 80, height: 80)
                                            .clipShape(RoundedRectangle(cornerRadius: 8))
                                    }
                                }
                            }
                        }
                    }
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
                            await viewModel.resolve(
                                complaintId: complaintId,
                                resolutionNotes: resolutionNotes,
                                token: token
                            )
                        }
                    } label: {
                        Text("Mark as Resolved")
                            .frame(maxWidth: .infinity, alignment: .center)
                            .bold()
                    }
                    .foregroundStyle(.white)
                    .listRowBackground(Color.green)
                    .disabled(resolutionNotes.trimmingCharacters(in: .whitespacesAndNewlines).isEmpty)
                }
            }
            .navigationTitle("Resolve Complaint")
            .navigationBarTitleDisplayMode(.inline)

        case .loading:
            ProgressView("Resolving…")
                .frame(maxWidth: .infinity, maxHeight: .infinity)
                .navigationTitle("Resolve Complaint")

        case .success:
            VStack(spacing: 16) {
                Image(systemName: "checkmark.shield.fill")
                    .font(.system(size: 60))
                    .foregroundStyle(.green)
                Text("Complaint Resolved")
                    .font(.title2.bold())
                Text("The resident will be notified to confirm the resolution.")
                    .multilineTextAlignment(.center)
                    .foregroundStyle(.secondary)
                    .padding(.horizontal)
                Button("Back to Tasks") {
                    router.popToRoot()
                }
                .buttonStyle(.borderedProminent)
            }
            .padding()
            .navigationTitle("Resolved")
            .navigationBarBackButtonHidden()
        }
    }
}
