export interface ExceptionDetails {
  exceptionId: number;
  dateCreated: string;
  shortDescription: string;
  nhsNumber?: string;
  name: string;
  dateOfBirth?: string;
  gender?: string;
  address?: string;
  contactDetails?: {
    phoneNumber?: string;
    email?: string;
  };
  serviceNowId?: string;
  serviceNowCreatedDate?: string;
}

export interface ReportDetails {
  reportId: string;
  dateCreated: string;
  description: string;
}
