export const ErrorCodes = {
  Auth: {
    InvalidCredentials: 'Auth.InvalidCredentials',
    TokenExpired: 'Auth.TokenExpired',
    TokenInvalid: 'Auth.TokenInvalid',
    MissingPropertyClaim: 'Auth.MissingPropertyClaim',
    AccountDeactivated: 'Auth.AccountDeactivated',
    InsufficientRole: 'Auth.InsufficientRole',
    InvitationTokenInvalid: 'Auth.InvitationTokenInvalid',
    InvitationTokenExpired: 'Auth.InvitationTokenExpired',
    InvitationTokenAlreadyUsed: 'Auth.InvitationTokenAlreadyUsed',
    InvitationTokenRevoked: 'Auth.InvitationTokenRevoked',
    EmailAlreadyRegistered: 'Auth.EmailAlreadyRegistered',
  },
  Complaint: {
    NotFound: 'Complaint.NotFound',
    AccessDenied: 'Complaint.AccessDenied',
    InvalidStatusTransition: 'Complaint.InvalidStatusTransition',
    AlreadyClosed: 'Complaint.AlreadyClosed',
    AlreadyResolved: 'Complaint.AlreadyResolved',
    NotAssigned: 'Complaint.NotAssigned',
    MaxMediaAttachmentsExceeded: 'Complaint.MaxMediaAttachmentsExceeded',
    InvalidMediaType: 'Complaint.InvalidMediaType',
    MediaFileTooLarge: 'Complaint.MediaFileTooLarge',
    StaffNotAvailable: 'Complaint.StaffNotAvailable',
    StaffBelongsToDifferentProperty: 'Complaint.StaffBelongsToDifferentProperty',
    FeedbackAlreadySubmitted: 'Complaint.FeedbackAlreadySubmitted',
    FeedbackNotAllowed: 'Complaint.FeedbackNotAllowed',
  },
  Staff: {
    NotFound: 'Staff.NotFound',
    CannotSetBusyManually: 'Staff.CannotSetBusyManually',
    NotAuthorizedToUpdateOtherStaff: 'Staff.NotAuthorizedToUpdateOtherStaff',
  },
  User: {
    NotFound: 'User.NotFound',
    AlreadyDeactivated: 'User.AlreadyDeactivated',
    AlreadyActive: 'User.AlreadyActive',
    CannotDeactivateSelf: 'User.CannotDeactivateSelf',
  },
  Outage: {
    NotFound: 'Outage.NotFound',
    EndTimeBeforeStartTime: 'Outage.EndTimeBeforeStartTime',
    StartTimeInPast: 'Outage.StartTimeInPast',
  },
  Dispatch: {
    NoStaffAvailable: 'Dispatch.NoStaffAvailable',
  },
  Validation: {
    Failed: 'Validation.Failed',
  },
  System: {
    InternalError: 'System.InternalError',
    StorageUnavailable: 'System.StorageUnavailable',
    DatabaseUnavailable: 'System.DatabaseUnavailable',
  },
} as const;

export type ErrorCode = typeof ErrorCodes[keyof typeof ErrorCodes][keyof typeof ErrorCodes[keyof typeof ErrorCodes]];
