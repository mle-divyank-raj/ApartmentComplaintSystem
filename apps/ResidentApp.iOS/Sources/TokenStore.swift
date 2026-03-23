import Foundation
import Security

// MARK: - TokenStore
// Stores JWT in the iOS Keychain. Publishes accessToken for UI reactivity.

@MainActor
final class TokenStore: ObservableObject {
    @Published private(set) var accessToken: String?
    @Published private(set) var userId: Int?
    @Published private(set) var role: String?
    @Published private(set) var propertyId: Int?

    private let service = "com.acls.resident"
    private let accountKey = "accessToken"
    private let userIdKey = "userId"
    private let roleKey = "role"
    private let propertyIdKey = "propertyId"

    init() {
        accessToken = read(account: accountKey)
        if let raw = read(account: userIdKey), let v = Int(raw) { userId = v }
        role = read(account: roleKey)
        if let raw = read(account: propertyIdKey), let v = Int(raw) { propertyId = v }
    }

    func save(response: AuthTokenResponse) {
        write(account: accountKey, value: response.accessToken)
        write(account: userIdKey, value: String(response.userId))
        write(account: roleKey, value: response.role)
        write(account: propertyIdKey, value: String(response.propertyId))
        accessToken = response.accessToken
        userId = response.userId
        role = response.role
        propertyId = response.propertyId
    }

    func clear() {
        delete(account: accountKey)
        delete(account: userIdKey)
        delete(account: roleKey)
        delete(account: propertyIdKey)
        accessToken = nil
        userId = nil
        role = nil
        propertyId = nil
    }

    // MARK: - Keychain helpers

    private func write(account: String, value: String) {
        let data = Data(value.utf8)
        let query: [CFString: Any] = [
            kSecClass: kSecClassGenericPassword,
            kSecAttrService: service,
            kSecAttrAccount: account
        ]
        SecItemDelete(query as CFDictionary)
        let attributes: [CFString: Any] = [
            kSecClass: kSecClassGenericPassword,
            kSecAttrService: service,
            kSecAttrAccount: account,
            kSecValueData: data
        ]
        SecItemAdd(attributes as CFDictionary, nil)
    }

    private func read(account: String) -> String? {
        let query: [CFString: Any] = [
            kSecClass: kSecClassGenericPassword,
            kSecAttrService: service,
            kSecAttrAccount: account,
            kSecReturnData: true,
            kSecMatchLimit: kSecMatchLimitOne
        ]
        var result: AnyObject?
        let status = SecItemCopyMatching(query as CFDictionary, &result)
        guard status == errSecSuccess, let data = result as? Data else { return nil }
        return String(data: data, encoding: .utf8)
    }

    private func delete(account: String) {
        let query: [CFString: Any] = [
            kSecClass: kSecClassGenericPassword,
            kSecAttrService: service,
            kSecAttrAccount: account
        ]
        SecItemDelete(query as CFDictionary)
    }
}
