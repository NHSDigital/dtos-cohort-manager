export interface ExceptionDetails {
  exceptionId: number;
  dateCreated: string;
  shortDescription: string;
  nhsNumber?: string;
  supersededByNhsNumber?: string;
  surname: string;
  forename: string;
  dateOfBirth?: string;
  gender?: number;
  address?: string;
  contactDetails?: {
    phoneNumber?: string;
    email?: string;
  };
  primaryCareProvider: string;
  serviceNowId?: string;
  serviceNowCreatedDate?: string;
}

export interface ReportDetails {
  reportId: string;
  dateCreated: string;
  description: string;
}
