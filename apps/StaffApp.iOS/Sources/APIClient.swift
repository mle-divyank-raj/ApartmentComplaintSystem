import Foundation

// MARK: - APIError

enum APIError: Error {
    case missingBaseURL
    case networkError(Error)
    case httpError(statusCode: Int, problemDetails: ProblemDetails?)
    case decodingError(Error)
    case unauthorized
}

// MARK: - ProblemDetails (RFC 7807)

struct ProblemDetails: Decodable {
    let type: String?
    let title: String?
    let status: Int?
    let detail: String?
    let errors: [String: [String]]?
}

// MARK: - MultipartFile

struct MultipartFile {
    let fieldName: String
    let fileName: String
    let mimeType: String
    let data: Data
}

// MARK: - APIClient

final class APIClient {
    static let shared = APIClient()

    private let session: URLSession
    private let decoder: JSONDecoder

    private init() {
        session = URLSession.shared
        decoder = JSONDecoder()
        decoder.keyDecodingStrategy = .convertFromSnakeCase
        decoder.dateDecodingStrategy = .iso8601
    }

    private var baseURL: String {
        get throws {
            guard let url = Bundle.main.object(forInfoDictionaryKey: "API_BASE_URL") as? String,
                  !url.isEmpty else {
                throw APIError.missingBaseURL
            }
            return url
        }
    }

    // MARK: - JSON Request

    func request<T: Decodable>(
        path: String,
        method: String = "GET",
        body: Encodable? = nil,
        token: String? = nil
    ) async throws -> T {
        let base = try baseURL
        guard let url = URL(string: base + path) else {
            throw APIError.missingBaseURL
        }

        var req = URLRequest(url: url)
        req.httpMethod = method
        req.setValue("application/json", forHTTPHeaderField: "Accept")

        if let token {
            req.setValue("Bearer \(token)", forHTTPHeaderField: "Authorization")
        }

        if let body {
            req.setValue("application/json", forHTTPHeaderField: "Content-Type")
            let encoder = JSONEncoder()
            encoder.keyEncodingStrategy = .convertToSnakeCase
            req.httpBody = try encoder.encode(body)
        }

        return try await execute(req)
    }

    // MARK: - Multipart Request

    func requestMultipart<T: Decodable>(
        path: String,
        method: String = "POST",
        fields: [String: String] = [:],
        fileFields: [MultipartFile] = [],
        token: String? = nil
    ) async throws -> T {
        let base = try baseURL
        guard let url = URL(string: base + path) else {
            throw APIError.missingBaseURL
        }

        let boundary = "Boundary-\(UUID().uuidString)"
        var req = URLRequest(url: url)
        req.httpMethod = method
        req.setValue("multipart/form-data; boundary=\(boundary)", forHTTPHeaderField: "Content-Type")
        req.setValue("application/json", forHTTPHeaderField: "Accept")

        if let token {
            req.setValue("Bearer \(token)", forHTTPHeaderField: "Authorization")
        }

        req.httpBody = buildMultipartBody(fields: fields, fileFields: fileFields, boundary: boundary)

        return try await execute(req)
    }

    private func buildMultipartBody(
        fields: [String: String],
        fileFields: [MultipartFile],
        boundary: String
    ) -> Data {
        var data = Data()
        let crlf = "\r\n"
        let dashdash = "--"

        for (name, value) in fields {
            data.append("\(dashdash)\(boundary)\(crlf)".data(using: .utf8)!)
            data.append("Content-Disposition: form-data; name=\"\(name)\"\(crlf)\(crlf)".data(using: .utf8)!)
            data.append("\(value)\(crlf)".data(using: .utf8)!)
        }

        for file in fileFields {
            data.append("\(dashdash)\(boundary)\(crlf)".data(using: .utf8)!)
            data.append("Content-Disposition: form-data; name=\"\(file.fieldName)\"; filename=\"\(file.fileName)\"\(crlf)".data(using: .utf8)!)
            data.append("Content-Type: \(file.mimeType)\(crlf)\(crlf)".data(using: .utf8)!)
            data.append(file.data)
            data.append(crlf.data(using: .utf8)!)
        }

        data.append("\(dashdash)\(boundary)\(dashdash)\(crlf)".data(using: .utf8)!)
        return data
    }

    // MARK: - Execute

    private func execute<T: Decodable>(_ req: URLRequest) async throws -> T {
        let (data, response): (Data, URLResponse)
        do {
            (data, response) = try await session.data(for: req)
        } catch {
            throw APIError.networkError(error)
        }

        guard let http = response as? HTTPURLResponse else {
            throw APIError.networkError(URLError(.badServerResponse))
        }

        guard (200..<300).contains(http.statusCode) else {
            if http.statusCode == 401 { throw APIError.unauthorized }
            let problem = try? decoder.decode(ProblemDetails.self, from: data)
            throw APIError.httpError(statusCode: http.statusCode, problemDetails: problem)
        }

        do {
            return try decoder.decode(T.self, from: data)
        } catch {
            throw APIError.decodingError(error)
        }
    }
}
