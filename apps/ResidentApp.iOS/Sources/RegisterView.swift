import SwiftUI

// MARK: - RegisterView
// Shown when user opens an invitation link. The invitationToken is passed in via deep link.

struct RegisterView: View {
    @EnvironmentObject private var tokenStore: TokenStore
    @StateObject private var viewModel = RegisterViewModel()

    let invitationToken: String

    @State private var email = ""
    @State private var password = ""
    @State private var firstName = ""
    @State private var lastName = ""
    @State private var phone = ""

    var body: some View {
        ScrollView {
            VStack(spacing: 20) {
                Text("Create Your Account")
                    .font(.title.bold())
                    .padding(.top, 24)

                Text("Complete your registration to access the resident portal.")
                    .font(.subheadline)
                    .foregroundStyle(.secondary)
                    .multilineTextAlignment(.center)

                VStack(spacing: 14) {
                    TextField("First Name", text: $firstName)
                        .textContentType(.givenName)
                        .textFieldStyle(.roundedBorder)

                    TextField("Last Name", text: $lastName)
                        .textContentType(.familyName)
                        .textFieldStyle(.roundedBorder)

                    TextField("Email", text: $email)
                        .keyboardType(.emailAddress)
                        .textContentType(.emailAddress)
                        .autocapitalization(.none)
                        .textFieldStyle(.roundedBorder)

                    SecureField("Password (min 8 characters)", text: $password)
                        .textContentType(.newPassword)
                        .textFieldStyle(.roundedBorder)

                    TextField("Phone (optional)", text: $phone)
                        .keyboardType(.phonePad)
                        .textContentType(.telephoneNumber)
                        .textFieldStyle(.roundedBorder)
                }

                if let errorMessage = viewModel.errorMessage {
                    Text(errorMessage)
                        .foregroundStyle(.red)
                        .font(.caption)
                        .multilineTextAlignment(.center)
                }

                Button {
                    Task {
                        await viewModel.register(
                            invitationToken: invitationToken,
                            email: email,
                            password: password,
                            firstName: firstName,
                            lastName: lastName,
                            phone: phone,
                            tokenStore: tokenStore
                        )
                    }
                } label: {
                    Group {
                        if viewModel.isLoading {
                            ProgressView().tint(.white)
                        } else {
                            Text("Create Account").fontWeight(.semibold)
                        }
                    }
                    .frame(maxWidth: .infinity)
                    .padding()
                    .background(Color.accentColor)
                    .foregroundStyle(.white)
                    .clipShape(RoundedRectangle(cornerRadius: 10))
                }
                .disabled(viewModel.isLoading)
            }
            .padding(.horizontal, 32)
            .padding(.bottom, 32)
        }
        .navigationTitle("Register")
        .navigationBarTitleDisplayMode(.inline)
    }
}
