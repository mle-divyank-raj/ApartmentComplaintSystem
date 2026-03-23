import { apiClient } from "@acls/sdk";
import type { DeclareOutageRequest, OutageDto } from "@acls/api-contracts";

export async function getAllOutages(): Promise<OutageDto[]> {
  const response = await apiClient.get<OutageDto[]>("/api/v1/outages");
  return response.data;
}

export async function getOutageById(outageId: number): Promise<OutageDto> {
  const response = await apiClient.get<OutageDto>(
    `/api/v1/outages/${outageId}`
  );
  return response.data;
}

export async function declareOutage(
  data: DeclareOutageRequest
): Promise<OutageDto> {
  const response = await apiClient.post<OutageDto>("/api/v1/outages", data);
  return response.data;
}
