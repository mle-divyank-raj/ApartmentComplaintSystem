import { apiClient } from "@acls/sdk";
import type {
  UserDto,
  InviteResidentRequest,
  InvitationDto,
} from "@acls/api-contracts";

export async function getAllUsers(): Promise<UserDto[]> {
  const response = await apiClient.get<UserDto[]>("/api/v1/users");
  return response.data;
}

export async function inviteResident(
  data: InviteResidentRequest
): Promise<InvitationDto> {
  const response = await apiClient.post<InvitationDto>(
    "/api/v1/users/invite",
    data
  );
  return response.data;
}

export async function deactivateUser(userId: number): Promise<void> {
  await apiClient.post(`/api/v1/users/${userId}/deactivate`);
}

export async function reactivateUser(userId: number): Promise<void> {
  await apiClient.post(`/api/v1/users/${userId}/reactivate`);
}
