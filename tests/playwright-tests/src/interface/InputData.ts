
interface Validation {
  NHSNumber: string;   // `other data services` returns NHSNumber
  NhsNumber: string;  // `api/ExceptionManagementDataService` returns NhsNumber
  apiEndpoint: string;
  PrimaryCareProvider: string;
  RuleId: number;
  RuleDescription: string;
  ExceptionFlag: number;

}

interface Validations {
  validations: Validation;
}

interface QueryParams {
  exceptionStatus: number;
  sortOrder: number;
  exceptionCategory: number;
}

export interface InputData {
  validations: Validations[];
  inputParticipantRecord: Record<string, any>;
  nhsNumbers: string[];
  queryParams: QueryParams;
}
