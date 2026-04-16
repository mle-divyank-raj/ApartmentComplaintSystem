// Complaint request/response types mirroring ACLS.Contracts + Application DTOs

export interface MediaDto {
  mediaId: number;
  url: string;
  type: string;
  uploadedAt: string;
}

export interface WorkNoteDto {
  workNoteId: number;
  note: string;
  createdByUserId: number;
  createdAt: string;
}

export interface StaffMemberSummaryDto {
  staffMemberId: number;
  fullName: string;
  jobTitle: string | null;
}

export interface ComplaintDto {
  complaintId: number;
  propertyId: number;
  unitId: number;
  unitNumber: string;
  buildingName: string;
  residentId: number;
  residentName: string;
  title: string;
  description: string;
  category: string;
  urgency: string;
  status: string;
  permissionToEnter: boolean;
  assignedStaffMember: StaffMemberSummaryDto | null;
  media: MediaDto[];
  workNotes: WorkNoteDto[];
  eta: string | null;
  createdAt: string;
  updatedAt: string;
  resolvedAt: string | null;
  tat: number | null;
  residentRating: number | null;
  residentFeedbackComment: string | null;
}

export interface ComplaintsPage {
  items: ComplaintSummaryDto[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface ComplaintSummaryDto {
  complaintId: number;
  unitId: number;
  unitNumber: string;
  buildingName: string;
  residentId: number;
  residentName: string;
  assignedStaffMemberId: number | null;
  assignedStaffMemberName: string | null;
  title: string;
  category: string;
  urgency: string;
  status: string;
  createdAt: string;
  updatedAt: string;
  resolvedAt: string | null;
}

export interface SubmitComplaintRequest {
  title: string;
  description: string;
  category: string;
  urgency: string;
  permissionToEnter: boolean;
}

export interface TriggerSosRequest {
  title: string;
  description: string;
  permissionToEnter: boolean;
}

export interface AssignComplaintRequest {
  staffMemberId: number;
}

export interface UpdateComplaintStatusRequest {
  status: string;
}

export interface ResolveComplaintRequest {
  resolutionNotes?: string;
}

export interface SubmitFeedbackRequest {
  rating: number;
  comment?: string;
}

export interface AddWorkNoteRequest {
  content: string;
}

export interface UpdateEtaRequest {
  eta: string;
}
