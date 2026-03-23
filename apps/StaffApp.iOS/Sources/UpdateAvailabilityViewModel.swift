import Foundation

// MARK: - UpdateAvailabilityViewModel

@MainActor
final class UpdateAvailabilityViewModel: ObservableObject {
    enum UiState {
        case idle
        case loading
        case success(newState: StaffState)
        case error(message: String)
    }

    @Published private(set) var uiState: UiState = .idle

    private let staffRepository: StaffRepository

    init(staffRepository: StaffRepository = StaffRepository()) {
        self.staffRepository = staffRepository
    }

    func updateAvailability(state: StaffState, token: String) async {
        uiState = .loading
        do {
            let updated = try await staffRepository.updateAvailability(state: state, token: token)
            uiState = .success(newState: updated.state)
        } catch let error as APIError {
            uiState = .error(message: ErrorCodeMapper.message(for: error))
        } catch {
            uiState = .error(message: ErrorCodeMapper.message(for: .networkError(error)))
        }
    }
}
