
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

export interface InputData {
  validations: Validations[];
  inputParticipantRecord: Record<string, any>;
  nhsNumbers: string[];
}
