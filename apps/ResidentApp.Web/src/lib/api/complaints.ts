import { apiClient } from "@acls/sdk";
import type {
  ComplaintDto,
  ComplaintsPage,
  AssignComplaintRequest,
  ResolveComplaintRequest,
  SubmitFeedbackRequest,
  AddWorkNoteRequest,
  UpdateEtaRequest,
} from "@acls/api-contracts";

export interface GetComplaintsParams {
  status?: string;
  urgency?: string;
  category?: string;
  search?: string;
  dateFrom?: string;
  dateTo?: string;
  assignedStaffId?: number;
  page?: number;
  pageSize?: number;
}

export async function getAllComplaints(
  params?: GetComplaintsParams
): Promise<ComplaintsPage> {
  const response = await apiClient.get<ComplaintsPage>("/api/v1/complaints", {
    params,
  });
  return response.data;
}

export async function getComplaintById(
  complaintId: number
): Promise<ComplaintDto> {
  const response = await apiClient.get<ComplaintDto>(
    `/api/v1/complaints/${complaintId}`
  );
  return response.data;
}

export async function assignComplaint(
  complaintId: number,
  data: AssignComplaintRequest
): Promise<ComplaintDto> {
  const response = await apiClient.post<ComplaintDto>(
    `/api/v1/complaints/${complaintId}/assign`,
    data
  );
  return response.data;
}

export async function reassignComplaint(
  complaintId: number,
  data: AssignComplaintRequest
): Promise<ComplaintDto> {
  const response = await apiClient.post<ComplaintDto>(
    `/api/v1/complaints/${complaintId}/reassign`,
    data
  );
  return response.data;
}

export async function resolveComplaint(
  complaintId: number,
  data: ResolveComplaintRequest
): Promise<ComplaintDto> {
  const response = await apiClient.post<ComplaintDto>(
    `/api/v1/complaints/${complaintId}/resolve`,
    data
  );
  return response.data;
}

export async function submitFeedback(
  complaintId: number,
  data: SubmitFeedbackRequest
): Promise<ComplaintDto> {
  const response = await apiClient.post<ComplaintDto>(
    `/api/v1/complaints/${complaintId}/feedback`,
    data
  );
  return response.data;
}

export async function addWorkNote(
  complaintId: number,
  data: AddWorkNoteRequest
): Promise<ComplaintDto> {
  const response = await apiClient.post<ComplaintDto>(
    `/api/v1/complaints/${complaintId}/work-notes`,
    data
  );
  return response.data;
}

export async function updateEta(
  complaintId: number,
  data: UpdateEtaRequest
): Promise<void> {
  await apiClient.post(`/api/v1/complaints/${complaintId}/eta`, data);
}
