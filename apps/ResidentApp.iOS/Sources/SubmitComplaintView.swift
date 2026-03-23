import SwiftUI
import PhotosUI

// MARK: - SubmitComplaintView

struct SubmitComplaintView: View {
    @EnvironmentObject private var tokenStore: TokenStore
    @EnvironmentObject private var router: NavigationRouter
    @StateObject private var viewModel = SubmitComplaintViewModel()

    @State private var title = ""
    @State private var description = ""
    @State private var selectedCategory: ComplaintCategory = .plumbing
    @State private var selectedUrgency: Urgency = .medium
    @State private var permissionToEnter = true

    var body: some View {
        ScrollView {
            VStack(alignment: .leading, spacing: 18) {

                // Category picker
                VStack(alignment: .leading, spacing: 6) {
                    Text("Category").font(.subheadline.weight(.medium))
                    Picker("Category", selection: $selectedCategory) {
                        ForEach(ComplaintCategory.allCases, id: \.self) { cat in
                            Text(cat.rawValue).tag(cat)
                        }
                    }
                    .pickerStyle(.menu)
                    .frame(maxWidth: .infinity, alignment: .leading)
                    .padding(8)
                    .background(Color(.systemGray6))
                    .clipShape(RoundedRectangle(cornerRadius: 8))
                }

                // Title
                VStack(alignment: .leading, spacing: 6) {
                    Text("Title").font(.subheadline.weight(.medium))
                    TextField("Brief description (max 200 chars)", text: $title)
                        .textFieldStyle(.roundedBorder)
                }

                // Description
                VStack(alignment: .leading, spacing: 6) {
                    Text("Description").font(.subheadline.weight(.medium))
                    TextEditor(text: $description)
                        .frame(minHeight: 100)
                        .padding(4)
                        .overlay(
                            RoundedRectangle(cornerRadius: 8)
                                .stroke(Color(.systemGray4))
                        )
                }

                // Urgency picker
                VStack(alignment: .leading, spacing: 6) {
                    Text("Urgency").font(.subheadline.weight(.medium))
                    Picker("Urgency", selection: $selectedUrgency) {
                        Text("Low").tag(Urgency.low)
                        Text("Medium").tag(Urgency.medium)
                        Text("High").tag(Urgency.high)
                    }
                    .pickerStyle(.segmented)
                }

                // Permission to enter toggle
                Toggle("Permission to Enter", isOn: $permissionToEnter)

                // Photo picker (up to 3)
                VStack(alignment: .leading, spacing: 8) {
                    Text("Photos (up to 3)").font(.subheadline.weight(.medium))
                    PhotosPicker(
                        selection: $viewModel.selectedPhotos,
                        maxSelectionCount: 3,
                        matching: .images
                    ) {
                        Label("Select Photos", systemImage: "photo.on.rectangle.angled")
                    }
                    .onChange(of: viewModel.selectedPhotos) { _ in
                        Task { await viewModel.loadSelectedPhotos() }
                    }

                    if !viewModel.selectedPhotoData.isEmpty {
                        ScrollView(.horizontal, showsIndicators: false) {
                            HStack(spacing: 8) {
                                ForEach(viewModel.selectedPhotoData.indices, id: \.self) { idx in
                                    if let uiImage = UIImage(data: viewModel.selectedPhotoData[idx]) {
                                        Image(uiImage: uiImage)
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

                // Error
                if case .error(let msg) = viewModel.uiState {
                    Text(msg)
                        .foregroundStyle(.red)
                        .font(.caption)
                }

                // Submit button
                Button {
                    guard let token = tokenStore.accessToken else { return }
                    Task {
                        await viewModel.submit(
                            title: title,
                            description: description,
                            category: selectedCategory,
                            urgency: selectedUrgency,
                            permissionToEnter: permissionToEnter,
                            token: token
                        )
                    }
                } label: {
                    Group {
                        if case .loading = viewModel.uiState {
                            ProgressView().tint(.white)
                        } else {
                            Text("Submit Complaint").fontWeight(.semibold)
                        }
                    }
                    .frame(maxWidth: .infinity)
                    .padding()
                    .background(Color.accentColor)
                    .foregroundStyle(.white)
                    .clipShape(RoundedRectangle(cornerRadius: 10))
                }
                .disabled({
                    if case .loading = viewModel.uiState { return true }
                    return false
                }())
            }
            .padding(16)
        }
        .navigationTitle("New Complaint")
        .navigationBarTitleDisplayMode(.inline)
        .onChange(of: viewModel.uiState) { state in
            if case .success(let complaint) = state {
                router.push(.submitComplaintConfirmation(complaintId: complaint.complaintId, title: complaint.title))
            }
        }
    }
}

// MARK: - Submit Confirmation View

struct SubmitComplaintConfirmationView: View {
    @EnvironmentObject private var router: NavigationRouter

    let complaintId: Int
    let title: String

    var body: some View {
        VStack(spacing: 24) {
            Spacer()

            Image(systemName: "checkmark.circle.fill")
                .font(.system(size: 72))
                .foregroundStyle(.green)

            Text("Complaint Submitted")
                .font(.title.bold())

            VStack(spacing: 8) {
                Text("Complaint #\(complaintId)")
                    .font(.headline)
                    .foregroundStyle(.secondary)
                Text(title)
                    .multilineTextAlignment(.center)
            }

            Button("View Status") {
                router.push(.complaintDetail(complaintId: complaintId))
            }
            .buttonStyle(.borderedProminent)

            Button("Back to Home") {
                router.popToRoot()
            }
            .foregroundStyle(.secondary)

            Spacer()
        }
        .padding(32)
        .navigationTitle("Submitted")
        .navigationBarTitleDisplayMode(.inline)
        .navigationBarBackButtonHidden(true)
    }
}
