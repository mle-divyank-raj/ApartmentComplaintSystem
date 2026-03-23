// Staff request/response types mirroring ACLS Application DTOs

import type { ComplaintDto } from "./complaints";

export interface StaffMemberDto {
  staffMemberId: number;
  userId: number;
  fullName: string;
  jobTitle: string | null;
  skills: string[];
  availability: string;
  averageRating: number | null;
  lastAssignedAt: string | null;
}

export interface StaffMemberWithAssignmentsDto extends StaffMemberDto {
  activeAssignments: ComplaintDto[];
}

export interface UpdateAvailabilityRequest {
  availability: string;
}
