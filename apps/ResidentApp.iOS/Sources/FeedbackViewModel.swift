import Foundation

// MARK: - FeedbackViewModel

@MainActor
final class FeedbackViewModel: ObservableObject {
    enum UiState {
        case idle
        case loading
        case success
        case error(message: String)
    }

    @Published private(set) var uiState: UiState = .idle

    private let repository: ComplaintRepository

    init(repository: ComplaintRepository = ComplaintRepository()) {
        self.repository = repository
    }

    func submitFeedback(
        complaintId: Int,
        rating: Int,
        comment: String,
        token: String
    ) async {
        uiState = .loading
        do {
            try await repository.submitFeedback(
                complaintId: complaintId,
                rating: rating,
                comment: comment.isEmpty ? nil : comment,
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
