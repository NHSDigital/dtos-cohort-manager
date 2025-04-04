
interface Validation {
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
