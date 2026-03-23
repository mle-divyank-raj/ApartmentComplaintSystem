import SwiftUI

// MARK: - FeedbackView

struct FeedbackView: View {
    let complaintId: Int

    @EnvironmentObject private var tokenStore: TokenStore
    @EnvironmentObject private var router: NavigationRouter
    @StateObject private var viewModel = FeedbackViewModel()

    @State private var rating = 0
    @State private var comment = ""

    var body: some View {
        switch viewModel.uiState {
        case .idle, .error:
            feedbackForm
        case .loading:
            ProgressView("Submitting…")
                .frame(maxWidth: .infinity, maxHeight: .infinity)
        case .success:
            ThankYouView()
        }
    }

    private var feedbackForm: some View {
        Form {
            Section("Your Experience") {
                VStack(alignment: .leading, spacing: 8) {
                    Text("Rating (required)")
                        .font(.subheadline)
                        .foregroundStyle(.secondary)
                    StarRatingView(rating: $rating)
                }
                .padding(.vertical, 4)
            }

            Section("Comment (optional)") {
                TextEditor(text: $comment)
                    .frame(minHeight: 100)
                    .onChange(of: comment) { newValue in
                        if newValue.count > 1000 {
                            comment = String(newValue.prefix(1000))
                        }
                    }
                Text("\(comment.count)/1000")
                    .font(.caption)
                    .foregroundStyle(.secondary)
                    .frame(maxWidth: .infinity, alignment: .trailing)
            }

            if case .error(let message) = viewModel.uiState {
                Section {
                    Text(message)
                        .foregroundStyle(.red)
                }
            }

            Section {
                Button {
                    guard let token = tokenStore.accessToken else { return }
                    Task {
                        await viewModel.submitFeedback(
                            complaintId: complaintId,
                            rating: rating,
                            comment: comment,
                            token: token
                        )
                    }
                } label: {
                    Text("Submit Feedback")
                        .frame(maxWidth: .infinity, alignment: .center)
                }
                .disabled(rating == 0)
            }
        }
        .navigationTitle("Leave Feedback")
        .navigationBarTitleDisplayMode(.inline)
    }
}

// MARK: - StarRatingView

struct StarRatingView: View {
    @Binding var rating: Int

    var body: some View {
        HStack(spacing: 8) {
            ForEach(1...5, id: \.self) { star in
                Button {
                    rating = star
                } label: {
                    Image(systemName: star <= rating ? "star.fill" : "star")
                        .font(.title2)
                        .foregroundStyle(star <= rating ? .yellow : .secondary)
                }
                .buttonStyle(.plain)
            }
        }
    }
}

// MARK: - ThankYouView

struct ThankYouView: View {
    @EnvironmentObject private var router: NavigationRouter

    var body: some View {
        VStack(spacing: 24) {
            Spacer()

            Image(systemName: "heart.fill")
                .font(.system(size: 72))
                .foregroundStyle(.pink)

            Text("Thank You!")
                .font(.title.bold())

            Text("Thank you for your feedback. Your complaint is now closed.")
                .multilineTextAlignment(.center)
                .foregroundStyle(.secondary)
                .padding(.horizontal)

            Button("Return Home") {
                router.popToRoot()
            }
            .buttonStyle(.borderedProminent)

            Spacer()
        }
        .padding()
        .navigationTitle("Feedback Submitted")
        .navigationBarTitleDisplayMode(.inline)
        .navigationBarBackButtonHidden()
    }
}
