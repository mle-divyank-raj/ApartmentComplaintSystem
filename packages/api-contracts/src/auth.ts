// Auth request/response types mirroring ACLS.Contracts Auth models

export interface RegisterResidentRequest {
  invitationToken: string;
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  phone?: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface AuthTokenResponse {
  accessToken: string;
  expiresAt: string;
  userId: number;
  role: string;
  propertyId: number;
}
