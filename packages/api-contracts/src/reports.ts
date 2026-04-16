// Reports response types mirroring ACLS Application DTOs

export interface ActiveAssignmentDto {
  complaintId: number;
  title: string;
  urgency: string;
  status: string;
  unitNumber: string;
  buildingName: string;
  assignedStaffMemberId: number;
  assignedStaffMemberName: string;
  eta: string | null;
  createdAt: string;
}

export interface StaffAvailabilitySummaryDto {
  staffMemberId: number;
  fullName: string;
  jobTitle: string | null;
  availability: string;
}

export interface DashboardMetricsDto {
  openCount: number;
  assignedCount: number;
  inProgressCount: number;
  resolvedCount: number;
  closedCount: number;
  sosActiveCount: number;
  activeAssignments: ActiveAssignmentDto[];
  staffAvailabilitySummary: StaffAvailabilitySummaryDto[];
}

export interface StaffPerformanceSummaryDto {
  staffMemberId: number;
  fullName: string;
  jobTitle: string | null;
  totalResolved: number;
  averageRating: number | null;
  averageTatMinutes: number | null;
}
