import Foundation
import PhotosUI
import SwiftUI

// MARK: - SubmitComplaintViewModel

@MainActor
final class SubmitComplaintViewModel: ObservableObject {
    enum UiState {
        case idle
        case loading
        case success(complaint: ComplaintDTO)
        case error(message: String)
    }

    @Published private(set) var uiState: UiState = .idle
    @Published var selectedPhotos: [PhotosPickerItem] = []
    @Published private(set) var selectedPhotoData: [Data] = []

    private let repository: ComplaintRepository

    init(repository: ComplaintRepository = ComplaintRepository()) {
        self.repository = repository
    }

    func loadSelectedPhotos() async {
        var loaded: [Data] = []
        for item in selectedPhotos {
            if let data = try? await item.loadTransferable(type: Data.self) {
                loaded.append(data)
            }
        }
        selectedPhotoData = loaded
    }

    func submit(
        title: String,
        description: String,
        category: ComplaintCategory,
        urgency: Urgency,
        permissionToEnter: Bool,
        token: String
    ) async {
        uiState = .loading

        let mediaFiles = selectedPhotoData.enumerated().map { idx, data in
            MultipartFile(
                fieldName: "mediaFiles",
                filename: "photo_\(idx).jpg",
                mimeType: "image/jpeg",
                data: data
            )
        }

        do {
            let complaint = try await repository.submitComplaint(
                title: title,
                description: description,
                category: category.rawValue,
                urgency: urgency,
                permissionToEnter: permissionToEnter,
                mediaFiles: mediaFiles,
                token: token
            )
            uiState = .success(complaint: complaint)
        } catch let error as APIError {
            uiState = .error(message: ErrorCodeMapper.message(for: error))
        } catch {
            uiState = .error(message: ErrorCodeMapper.message(for: .networkError(error)))
        }
    }
}
