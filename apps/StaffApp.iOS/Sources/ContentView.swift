import SwiftUI

struct ContentView: View {
    @EnvironmentObject private var tokenStore: TokenStore

    var body: some View {
        if tokenStore.accessToken != nil {
            HomeView()
        } else {
            LoginView()
        }
    }
}
