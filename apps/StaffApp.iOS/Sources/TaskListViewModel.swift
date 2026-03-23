import Foundation

// MARK: - TaskListViewModel

@MainActor
final class TaskListViewModel: ObservableObject {
    enum UiState {
        case idle
        case loading
        case success(profile: StaffMemberDTO)
        case error(message: String)
    }

    @Published private(set) var uiState: UiState = .idle

    private let staffRepository: StaffRepository

    init(staffRepository: StaffRepository = StaffRepository()) {
        self.staffRepository = staffRepository
    }

    func loadProfile(token: String) async {
        uiState = .loading
        do {
            let profile = try await staffRepository.getMyProfile(token: token)
            uiState = .success(profile: profile)
        } catch let error as APIError {
            uiState = .error(message: ErrorCodeMapper.message(for: error))
        } catch {
            uiState = .error(message: ErrorCodeMapper.message(for: .networkError(error)))
        }
    }
}
