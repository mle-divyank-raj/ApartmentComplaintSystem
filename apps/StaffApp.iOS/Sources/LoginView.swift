import SwiftUI

// MARK: - LoginView

struct LoginView: View {
    @EnvironmentObject private var tokenStore: TokenStore
    @StateObject private var viewModel = LoginViewModel()

    @State private var email = ""
    @State private var password = ""

    var body: some View {
        VStack(spacing: 32) {
            Spacer()

            VStack(spacing: 8) {
                Image(systemName: "wrench.and.screwdriver.fill")
                    .font(.system(size: 60))
                    .foregroundStyle(.blue)
                Text("Staff Portal")
                    .font(.largeTitle.bold())
                Text("ACLS Maintenance")
                    .font(.subheadline)
                    .foregroundStyle(.secondary)
            }

            VStack(spacing: 16) {
                TextField("Email", text: $email)
                    .textInputAutocapitalization(.never)
                    .keyboardType(.emailAddress)
                    .autocorrectionDisabled()
                    .textFieldStyle(.roundedBorder)

                SecureField("Password", text: $password)
                    .textFieldStyle(.roundedBorder)

                if case .error(let message) = viewModel.uiState {
                    Text(message)
                        .font(.caption)
                        .foregroundStyle(.red)
                        .multilineTextAlignment(.center)
                }

                Button {
                    Task {
                        await viewModel.login(
                            email: email,
                            password: password,
                            tokenStore: tokenStore
                        )
                    }
                } label: {
                    Group {
                        if case .loading = viewModel.uiState {
                            ProgressView()
                                .tint(.white)
                        } else {
                            Text("Sign In")
                                .bold()
                        }
                    }
                    .frame(maxWidth: .infinity)
                    .padding(.vertical, 4)
                }
                .buttonStyle(.borderedProminent)
                .disabled(email.isEmpty || password.isEmpty || {
                    if case .loading = viewModel.uiState { return true }
                    return false
                }())
            }
            .padding(.horizontal)

            Spacer()
        }
        .padding()
    }
}
