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
  gpPracticeCode?: string;
  gpPracticeAddress?: string;
}
