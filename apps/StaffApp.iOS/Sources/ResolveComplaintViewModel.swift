import Foundation
import PhotosUI

// MARK: - ResolveComplaintViewModel

@MainActor
final class ResolveComplaintViewModel: ObservableObject {
    enum UiState {
        case idle
        case loading
        case success
        case error(message: String)
    }

    @Published private(set) var uiState: UiState = .idle
    @Published var selectedPhotos: [PhotosPickerItem] = []
    @Published private(set) var photoDataList: [Data] = []

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
        photoDataList = loaded
    }

    func resolve(complaintId: Int, resolutionNotes: String, token: String) async {
        uiState = .loading
        let photos = photoDataList.enumerated().map { index, data in
            MultipartFile(
                fieldName: "completionPhotos",
                fileName: "photo_\(index + 1).jpg",
                mimeType: "image/jpeg",
                data: data
            )
        }
        do {
            _ = try await repository.resolveComplaint(
                complaintId: complaintId,
                resolutionNotes: resolutionNotes,
                completionPhotos: photos,
                token: token
            )
            uiState = .success
        } catch let error as APIError {
            uiState = .error(message: ErrorCodeMapper.message(for: error))
        } catch {
            uiState = .error(message: ErrorCodeMapper.message(for: .networkError(error)))
        }
    }
}
