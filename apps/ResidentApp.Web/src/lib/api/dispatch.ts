import { apiClient } from "@acls/sdk";
import type { DispatchRecommendationsResponse } from "@acls/api-contracts";

export async function getDispatchRecommendations(
  complaintId: number
): Promise<DispatchRecommendationsResponse> {
  const response = await apiClient.get<DispatchRecommendationsResponse>(
    `/api/v1/dispatch/recommendations/${complaintId}`
  );
  return response.data;
}
