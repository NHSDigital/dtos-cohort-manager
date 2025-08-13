export interface ExceptionsAPI {
  ExceptionId: number;
  NhsNumber: string;
  DateCreated: string;
  DateResolved: string;
  RuleId: number;
  RuleDescription: string;
  Category: number;
  ScreeningName: string;
  ExceptionDate: string;
  CohortName: string;
  Fatal: number;
  ServiceNowId: string;
  ServiceNowCreatedDate: string;
  RecordUpdatedDate: string;
}

export interface ExceptionAPIDetails extends ExceptionsAPI {
  ExceptionDetails: {
    GivenName: string;
    FamilyName: string;
    DateOfBirth: string;
    Gender: number;
    ParticipantAddressLine1: string;
    ParticipantAddressLine2: string;
    ParticipantAddressLine3: string;
    ParticipantAddressLine4: string;
    ParticipantAddressLine5: string;
    ParticipantPostCode: string;
    TelephoneNumberHome: string;
    EmailAddressHome: string;
    PrimaryCareProvider: string;
    SupersededByNhsNumber?: string;
  };
}
