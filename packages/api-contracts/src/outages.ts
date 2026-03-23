// Outage request/response types mirroring ACLS.Contracts + Application DTOs

export interface DeclareOutageRequest {
  title: string;
  outageType: string;
  description: string;
  startTime: string;
  endTime?: string;
}

export interface OutageDto {
  outageId: number;
  propertyId: number;
  title: string;
  outageType: string;
  description: string;
  startTime: string;
  endTime: string | null;
  declaredAt: string;
  notificationSentAt: string | null;
}
