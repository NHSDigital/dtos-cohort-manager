export interface ExceptionDetails {
  exceptionId: number;
  dateCreated: string;
  shortDescription: string;
  moreDetails?: string;
  reportingId?: string;
  portalFormTitle?: string;
  nhsNumber?: string;
  supersededByNhsNumber?: string;
  surname?: string;
  forename?: string;
  dateOfBirth?: string;
  gender?: number;
  address?: string;
  addressParts?: string[];
  contactDetails?: {
    phoneNumber?: string;
    email?: string;
  };
  primaryCareProvider?: string;
  serviceNowId?: string;
  serviceNowCreatedDate?: string;
}

export interface RuleMapping {
  ruleDescription: string;
  moreDetails?: string;
  reportingId?: string;
  portalFormTitle?: string;
}

export interface ReportDetails {
  reportId: string;
  dateCreated: string;
  category: number;
}
