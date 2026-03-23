// Reports response types mirroring ACLS Application DTOs

import type { StaffMemberSummaryDto } from "./complaints";

export interface ActiveAssignmentDto {
  complaintId: number;
  title: string;
  urgency: string;
  status: string;
  unitNumber: string;
  buildingName: string;
  assignedStaffMember: StaffMemberSummaryDto;
}

export interface DashboardMetricsDto {
  openCount: number;
  assignedCount: number;
  inProgressCount: number;
  resolvedCount: number;
  closedCount: number;
  sosActiveCount: number;
  activeAssignments: ActiveAssignmentDto[];
  staffAvailabilitySummary: StaffMemberSummaryDto[];
}

export interface StaffPerformanceSummaryDto {
  staffMemberId: number;
  fullName: string;
  jobTitle: string | null;
  totalResolved: number;
  averageRating: number | null;
  averageTatMinutes: number | null;
}
