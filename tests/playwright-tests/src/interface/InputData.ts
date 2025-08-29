
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

interface ServiceNowRequestValidation {
  caseNumber: string;
  messageType: 1|2|3;
}

export interface ServiceNowRequestValidations {
  validation: ServiceNowRequestValidation
}

export interface InputData {
  validations: Validations[];
  serviceNowRequestValidations: ServiceNowRequestValidations[];
  inputParticipantRecord: Record<string, any>;
  nhsNumbers: string[];
  queryParams: QueryParams;
}

export interface ParticipantRecord {
  number: string;
  u_case_variable_data: {
    nhs_number: string;
    forename_: string;
    surname_family_name: string;
    date_of_birth: string;
    enter_dummy_gp_code: string;
    BSO_code: string;
    reason_for_adding: string;
  };
}
