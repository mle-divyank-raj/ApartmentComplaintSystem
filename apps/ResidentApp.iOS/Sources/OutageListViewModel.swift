import Foundation

// MARK: - OutageListViewModel

@MainActor
final class OutageListViewModel: ObservableObject {
    enum UiState {
        case idle
        case loading
        case success(outages: [OutageDTO])
        case empty
        case error(message: String)
    }

    @Published private(set) var uiState: UiState = .idle

    private let repository: OutageRepository

    init(repository: OutageRepository = OutageRepository()) {
        self.repository = repository
    }

    func loadOutages(token: String) async {
        uiState = .loading
        do {
            let outages = try await repository.getOutages(token: token)
            uiState = outages.isEmpty ? .empty : .success(outages: outages)
        } catch let error as APIError {
            uiState = .error(message: ErrorCodeMapper.message(for: error))
        } catch {
            uiState = .error(message: ErrorCodeMapper.message(for: .networkError(error)))
        }
    }
}
