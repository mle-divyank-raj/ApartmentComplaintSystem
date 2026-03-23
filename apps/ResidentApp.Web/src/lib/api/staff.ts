import { apiClient } from "@acls/sdk";
import type {
  StaffMemberDto,
  StaffMemberWithAssignmentsDto,
} from "@acls/api-contracts";

export async function getAllStaff(): Promise<StaffMemberDto[]> {
  const response = await apiClient.get<StaffMemberDto[]>("/api/v1/staff");
  return response.data;
}

export async function getAvailableStaff(): Promise<StaffMemberDto[]> {
  const response = await apiClient.get<StaffMemberDto[]>(
    "/api/v1/staff/available"
  );
  return response.data;
}

export async function getStaffById(
  staffMemberId: number
): Promise<StaffMemberWithAssignmentsDto> {
  const response = await apiClient.get<StaffMemberWithAssignmentsDto>(
    `/api/v1/staff/${staffMemberId}`
  );
  return response.data;
}

export async function getMyStaffProfile(): Promise<StaffMemberWithAssignmentsDto> {
  const response =
    await apiClient.get<StaffMemberWithAssignmentsDto>("/api/v1/staff/me");
  return response.data;
}
