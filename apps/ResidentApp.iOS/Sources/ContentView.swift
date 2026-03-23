import SwiftUI

struct ContentView: View {
    @EnvironmentObject private var router: NavigationRouter
    @EnvironmentObject private var tokenStore: TokenStore

    var body: some View {
        Group {
            if tokenStore.accessToken != nil {
                HomeView()
            } else {
                LoginView()
            }
        }
    }
}
