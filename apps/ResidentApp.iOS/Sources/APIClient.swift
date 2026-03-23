import Foundation

// MARK: - ProblemDetails (RFC 7807)

struct ProblemDetails: Decodable {
    let type: String?
    let title: String?
    let status: Int
    let detail: String?
    let errorCode: String?
    let errors: [String: [String]]?
}

// MARK: - APIError

enum APIError: Error {
    case networkError(Error)
    case httpError(statusCode: Int, problemDetails: ProblemDetails?)
    case decodingError(Error)
    case missingBaseURL
    case unauthorized
}

// MARK: - APIClient

final class APIClient {
    static let shared = APIClient()

    private let session: URLSession
    private let decoder: JSONDecoder

    private var baseURL: URL {
        guard
            let urlString = Bundle.main.object(forInfoDictionaryKey: "API_BASE_URL") as? String,
            let url = URL(string: urlString)
        else {
            // Should never happen if Info.plist is configured correctly
            fatalError("API_BASE_URL missing or malformed in Info.plist")
        }
        return url
    }

    private init() {
        let config = URLSessionConfiguration.default
        config.timeoutIntervalForRequest = 30
        self.session = URLSession(configuration: config)

        let dec = JSONDecoder()
        dec.keyDecodingStrategy = .convertFromSnakeCase
        dec.dateDecodingStrategy = .iso8601
        self.decoder = dec
    }

    // MARK: - Generic request

    func request<T: Decodable>(
        path: String,
        method: String = "GET",
        body: Encodable? = nil,
        token: String? = nil
    ) async throws -> T {
        let url = baseURL.appendingPathComponent(path)
        var req = URLRequest(url: url)
        req.httpMethod = method
        req.setValue("application/json", forHTTPHeaderField: "Content-Type")
        req.setValue("application/json", forHTTPHeaderField: "Accept")

        if let token {
            req.setValue("Bearer \(token)", forHTTPHeaderField: "Authorization")
        }

        if let body {
            let encoder = JSONEncoder()
            encoder.keyEncodingStrategy = .convertToSnakeCase
            req.httpBody = try encoder.encode(body)
        }

        return try await performRequest(req)
    }

    // MARK: - Multipart request

    func requestMultipart<T: Decodable>(
        path: String,
        method: String = "POST",
        fields: [String: String],
        fileFields: [String: [MultipartFile]],
        token: String? = nil
    ) async throws -> T {
        let url = baseURL.appendingPathComponent(path)
        var req = URLRequest(url: url)
        req.httpMethod = method

        let boundary = "ACLS_\(UUID().uuidString.replacingOccurrences(of: "-", with: ""))"
        req.setValue("multipart/form-data; boundary=\(boundary)", forHTTPHeaderField: "Content-Type")
        req.setValue("application/json", forHTTPHeaderField: "Accept")

        if let token {
            req.setValue("Bearer \(token)", forHTTPHeaderField: "Authorization")
        }

        req.httpBody = buildMultipartBody(boundary: boundary, fields: fields, fileFields: fileFields)

        return try await performRequest(req)
    }

    // MARK: - Internal

    private func performRequest<T: Decodable>(_ req: URLRequest) async throws -> T {
        let (data, response) = try await session.data(for: req)
        guard let httpResponse = response as? HTTPURLResponse else {
            throw APIError.networkError(URLError(.badServerResponse))
        }

        guard (200...299).contains(httpResponse.statusCode) else {
            let problemDetails = try? decoder.decode(ProblemDetails.self, from: data)
            if httpResponse.statusCode == 401 {
                throw APIError.unauthorized
            }
            throw APIError.httpError(statusCode: httpResponse.statusCode, problemDetails: problemDetails)
        }

        do {
            return try decoder.decode(T.self, from: data)
        } catch {
            throw APIError.decodingError(error)
        }
    }

    private func buildMultipartBody(
        boundary: String,
        fields: [String: String],
        fileFields: [String: [MultipartFile]]
    ) -> Data {
        var body = Data()
        let crlf = "\r\n"
        let boundaryPrefix = "--\(boundary)\(crlf)"

        for (key, value) in fields {
            body.append(boundaryPrefix.toData())
            body.append("Content-Disposition: form-data; name=\"\(key)\"\(crlf)\(crlf)".toData())
            body.append("\(value)\(crlf)".toData())
        }

        for (fieldName, files) in fileFields {
            for file in files {
                body.append(boundaryPrefix.toData())
                body.append("Content-Disposition: form-data; name=\"\(fieldName)\"; filename=\"\(file.filename)\"\(crlf)".toData())
                body.append("Content-Type: \(file.mimeType)\(crlf)\(crlf)".toData())
                body.append(file.data)
                body.append(crlf.toData())
            }
        }

        body.append("--\(boundary)--\(crlf)".toData())
        return body
    }
}

// MARK: - MultipartFile

struct MultipartFile {
    let fieldName: String
    let filename: String
    let mimeType: String
    let data: Data
}

// MARK: - Helpers

private extension String {
    func toData() -> Data {
        Data(self.utf8)
    }
}
