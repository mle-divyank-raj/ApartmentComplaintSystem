import Foundation
import Security

// MARK: - TokenStore

@MainActor
final class TokenStore: ObservableObject {
    @Published private(set) var accessToken: String?
    @Published private(set) var userId: Int?
    @Published private(set) var role: String?
    @Published private(set) var staffMemberId: Int?

    private let service = "com.acls.staff"

    init() {
        loadFromKeychain()
    }

    func save(response: AuthTokenResponse) {
        accessToken = response.accessToken
        userId = response.userId
        role = response.role
        staffMemberId = response.staffMemberId

        storeInKeychain(key: "accessToken", value: response.accessToken)
        if let uid = response.userId {
            storeInKeychain(key: "userId", value: String(uid))
        }
        if let r = response.role {
            storeInKeychain(key: "role", value: r)
        }
        if let sid = response.staffMemberId {
            storeInKeychain(key: "staffMemberId", value: String(sid))
        }
    }

    func clear() {
        accessToken = nil
        userId = nil
        role = nil
        staffMemberId = nil
        deleteFromKeychain(key: "accessToken")
        deleteFromKeychain(key: "userId")
        deleteFromKeychain(key: "role")
        deleteFromKeychain(key: "staffMemberId")
    }

    // MARK: - Private Keychain Helpers

    private func loadFromKeychain() {
        accessToken = loadFromKeychain(key: "accessToken")
        if let uid = loadFromKeychain(key: "userId") { userId = Int(uid) }
        role = loadFromKeychain(key: "role")
        if let sid = loadFromKeychain(key: "staffMemberId") { staffMemberId = Int(sid) }
    }

    private func storeInKeychain(key: String, value: String) {
        let data = Data(value.utf8)
        let query: [CFString: Any] = [
            kSecClass: kSecClassGenericPassword,
            kSecAttrService: service,
            kSecAttrAccount: key
        ]
        SecItemDelete(query as CFDictionary)
        let attrs: [CFString: Any] = [
            kSecClass: kSecClassGenericPassword,
            kSecAttrService: service,
            kSecAttrAccount: key,
            kSecValueData: data
        ]
        SecItemAdd(attrs as CFDictionary, nil)
    }

    private func loadFromKeychain(key: String) -> String? {
        let query: [CFString: Any] = [
            kSecClass: kSecClassGenericPassword,
            kSecAttrService: service,
            kSecAttrAccount: key,
            kSecReturnData: true,
            kSecMatchLimit: kSecMatchLimitOne
        ]
        var result: AnyObject?
        let status = SecItemCopyMatching(query as CFDictionary, &result)
        guard status == errSecSuccess,
              let data = result as? Data,
              let value = String(data: data, encoding: .utf8) else { return nil }
        return value
    }

    private func deleteFromKeychain(key: String) {
        let query: [CFString: Any] = [
            kSecClass: kSecClassGenericPassword,
            kSecAttrService: service,
            kSecAttrAccount: key
        ]
        SecItemDelete(query as CFDictionary)
    }
}
