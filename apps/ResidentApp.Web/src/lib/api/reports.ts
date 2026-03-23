import { apiClient } from "@acls/sdk";
import type {
  DashboardMetricsDto,
  StaffPerformanceSummaryDto,
} from "@acls/api-contracts";
import type { ComplaintDto } from "@acls/api-contracts";

export async function getDashboardMetrics(): Promise<DashboardMetricsDto> {
  const response = await apiClient.get<DashboardMetricsDto>(
    "/api/v1/reports/dashboard"
  );
  return response.data;
}

export async function getStaffPerformance(): Promise<
  StaffPerformanceSummaryDto[]
> {
  const response = await apiClient.get<StaffPerformanceSummaryDto[]>(
    "/api/v1/reports/staff-performance"
  );
  return response.data;
}

export async function getUnitHistory(
  unitId: number
): Promise<ComplaintDto[]> {
  const response = await apiClient.get<ComplaintDto[]>(
    `/api/v1/reports/unit-history/${unitId}`
  );
  return response.data;
}

export async function getComplaintsSummary(params?: {
  status?: string;
  urgency?: string;
}): Promise<ComplaintDto[]> {
  const response = await apiClient.get<ComplaintDto[]>(
    "/api/v1/reports/complaints-summary",
    { params }
  );
  return response.data;
}
