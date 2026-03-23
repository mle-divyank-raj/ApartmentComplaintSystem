export type { RegisterResidentRequest, LoginRequest, AuthTokenResponse } from "./auth";

export type {
  MediaDto,
  WorkNoteDto,
  StaffMemberSummaryDto,
  ComplaintDto,
  ComplaintsPage,
  SubmitComplaintRequest,
  TriggerSosRequest,
  AssignComplaintRequest,
  UpdateComplaintStatusRequest,
  ResolveComplaintRequest,
  SubmitFeedbackRequest,
  AddWorkNoteRequest,
  UpdateEtaRequest,
} from "./complaints";

export type {
  StaffMemberDto,
  StaffMemberWithAssignmentsDto,
  UpdateAvailabilityRequest,
} from "./staff";

export type { StaffScoreDto, DispatchRecommendationsResponse } from "./dispatch";

export type { DeclareOutageRequest, OutageDto } from "./outages";

export type {
  ActiveAssignmentDto,
  DashboardMetricsDto,
  StaffPerformanceSummaryDto,
} from "./reports";

export type {
  UserDto,
  InviteResidentRequest,
  InvitationDto,
} from "./users";
