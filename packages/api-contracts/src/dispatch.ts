// Dispatch response types mirroring ACLS Application DTOs

export interface StaffScoreDto {
  staffMemberId: number;
  fullName: string;
  jobTitle: string | null;
  skills: string[];
  availability: string;
  matchScore: number;
  skillScore: number;
  idleScore: number;
  averageRating: number | null;
  lastAssignedAt: string | null;
}

export interface DispatchRecommendationsResponse {
  complaintId: number;
  recommendations: StaffScoreDto[];
}
