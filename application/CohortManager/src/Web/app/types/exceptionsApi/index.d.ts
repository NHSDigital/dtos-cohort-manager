export interface ExceptionsAPI {
  ExceptionId: number;
  FileName: string;
  NhsNumber: string;
  DateCreated: string;
  DateResolved: string;
  RuleId: number;
  RuleDescription: string;
  ErrorRecord: string;
  Category: number;
  ScreeningName: string;
  ExceptionDate: string;
  CohortName: string;
  Fatal: number;
}

export interface ExceptionAPIDetails extends ExceptionsAPI {
  ExceptionDetails: {
    GivenName: string;
    FamilyName: string;
    DateOfBirth: string;
    Gender: string;
    ParticipantAddressLine1: string;
    ParticipantAddressLine2: string;
    ParticipantAddressLine3: string;
    ParticipantAddressLine4: string;
    ParticipantAddressLine5: string;
    ParticipantPostCode: string;
    TelephoneNumberHome: string;
    EmailAddressHome: string;
    GpPracticeCode: string;
    GpAddressLine1: string;
    GpAddressLine2: string;
    GpAddressLine3: string;
    GpAddressLine4: string;
    GpAddressLine5: string;
    GpPostCode: string;
  };
}
