import SwiftUI

@main
struct ACLSStaffApp: App {
    @StateObject private var tokenStore = TokenStore()
    @StateObject private var router = NavigationRouter()

    var body: some Scene {
        WindowGroup {
            ContentView()
                .environmentObject(tokenStore)
                .environmentObject(router)
        }
    }
}
