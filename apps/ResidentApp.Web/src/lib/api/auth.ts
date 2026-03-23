import { apiClient, storeToken } from "@acls/sdk";
import type {
  LoginRequest,
  AuthTokenResponse,
} from "@acls/api-contracts";

export async function login(data: LoginRequest): Promise<AuthTokenResponse> {
  const response = await apiClient.post<AuthTokenResponse>(
    "/api/v1/auth/login",
    data
  );
  storeToken(response.data.accessToken);
  return response.data;
}
