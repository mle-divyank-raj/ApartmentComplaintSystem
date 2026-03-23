import SwiftUI

@main
struct ACLSApp: App {
    @StateObject private var router = NavigationRouter()
    @StateObject private var tokenStore = TokenStore()

    var body: some Scene {
        WindowGroup {
            ContentView()
                .environmentObject(router)
                .environmentObject(tokenStore)
        }
    }
}
