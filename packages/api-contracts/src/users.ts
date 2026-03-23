// User request/response types mirroring ACLS.Contracts + Application DTOs

export interface UserDto {
  userId: number;
  email: string;
  firstName: string;
  lastName: string;
  role: string;
  isActive: boolean;
  createdAt: string;
  lastLoginAt: string | null;
}

export interface InviteResidentRequest {
  email: string;
  unitId: number;
}

export interface InvitationDto {
  invitationTokenId: number;
  email: string;
  unitId: number;
  unitNumber: string;
  expiresAt: string;
  issuedAt: string;
}
