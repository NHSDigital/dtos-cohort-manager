
interface Validation {
  nhsNumber: string;
  screeningServiceId: string,
  tableName: string;
  columnName: string;
  columnValue: string;
}

interface Validations {
  validations: Validation;
}

export interface InputData {
  validations: Validations[];
}
