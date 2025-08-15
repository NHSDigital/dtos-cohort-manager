import { ExceptionAPIDetails } from "@/app/types/exceptionsApi";
import mockDataJson from "@/app/data/mockExceptions.json";

// Type for the simplified list items in the JSON
type ExceptionListItem = {
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
};

// Shared in-memory data store that both APIs can read from and write to
class MockDataStore {
  private static instance: MockDataStore;
  private exceptions: Record<number, ExceptionAPIDetails>;
  private notRaisedExceptions: ExceptionListItem[];
  private raisedExceptions: ExceptionListItem[];

  private constructor() {
    // Initialize with data from JSON file
    this.exceptions = { ...mockDataJson.exceptions };
    this.notRaisedExceptions = [...mockDataJson.notRaisedExceptions];
    this.raisedExceptions = [...mockDataJson.raisedExceptions];
  }

  public static getInstance(): MockDataStore {
    if (!MockDataStore.instance) {
      MockDataStore.instance = new MockDataStore();
    }
    return MockDataStore.instance;
  }

  // Getters
  public getExceptions(): Record<number, ExceptionAPIDetails> {
    return this.exceptions;
  }

  public getException(id: number): ExceptionAPIDetails | undefined {
    return this.exceptions[id];
  }

  public getNotRaisedExceptions(): ExceptionListItem[] {
    return this.notRaisedExceptions;
  }

  public getRaisedExceptions(): ExceptionListItem[] {
    return this.raisedExceptions;
  }

  // Update methods
  public updateExceptionServiceNow(
    exceptionId: number,
    serviceNowId: string
  ): boolean {
    const exception = this.exceptions[exceptionId];
    if (!exception) {
      return false;
    }

    const oldServiceNowId = exception.ServiceNowId;

    // Update the main exceptions record
    exception.ServiceNowId = serviceNowId;
    exception.RecordUpdatedDate = new Date().toISOString();

    // Update ServiceNowCreatedDate if we're setting a ServiceNow ID for the first time
    if (!oldServiceNowId && serviceNowId) {
      exception.ServiceNowCreatedDate = new Date().toISOString();
    } else if (oldServiceNowId && !serviceNowId) {
      // Clear ServiceNowCreatedDate if we're removing the ServiceNow ID
      exception.ServiceNowCreatedDate = "";
    }

    // Update the corresponding record in notRaisedExceptions or raisedExceptions
    this.updateListExceptions(exception, oldServiceNowId, serviceNowId);

    return true;
  }

  private updateListExceptions(
    exception: ExceptionAPIDetails,
    oldServiceNowId: string,
    newServiceNowId: string
  ): void {
    const wasRaised = !!oldServiceNowId;
    const isNowRaised = !!newServiceNowId;

    // Create a simplified version for the list
    const listItem: ExceptionListItem = {
      ExceptionId: exception.ExceptionId,
      NhsNumber: exception.NhsNumber,
      DateCreated: exception.DateCreated,
      DateResolved: exception.DateResolved,
      RuleId: exception.RuleId,
      RuleDescription: exception.RuleDescription,
      Category: exception.Category,
      ScreeningName: exception.ScreeningName,
      ExceptionDate: exception.ExceptionDate,
      CohortName: exception.CohortName,
      Fatal: exception.Fatal,
      ServiceNowId: exception.ServiceNowId,
      ServiceNowCreatedDate: exception.ServiceNowCreatedDate,
      RecordUpdatedDate: exception.RecordUpdatedDate,
    };

    if (wasRaised === isNowRaised) {
      // Status didn't change, just update the existing record
      if (isNowRaised) {
        // Update in raisedExceptions
        const index = this.raisedExceptions.findIndex(
          (e) => e.ExceptionId === exception.ExceptionId
        );
        if (index !== -1) {
          this.raisedExceptions[index] = listItem;
        }
      } else {
        // Update in notRaisedExceptions
        const index = this.notRaisedExceptions.findIndex(
          (e) => e.ExceptionId === exception.ExceptionId
        );
        if (index !== -1) {
          this.notRaisedExceptions[index] = listItem;
        }
      }
    } else {
      // Status changed, move between lists
      if (isNowRaised) {
        // Move from notRaised to raised
        this.notRaisedExceptions = this.notRaisedExceptions.filter(
          (e) => e.ExceptionId !== exception.ExceptionId
        );
        this.raisedExceptions.push(listItem);
      } else {
        // Move from raised to notRaised
        this.raisedExceptions = this.raisedExceptions.filter(
          (e) => e.ExceptionId !== exception.ExceptionId
        );
        this.notRaisedExceptions.push(listItem);
      }
    }
  }

  // Reset to original state (useful for testing)
  public reset(): void {
    this.exceptions = { ...mockDataJson.exceptions };
    this.notRaisedExceptions = [...mockDataJson.notRaisedExceptions];
    this.raisedExceptions = [...mockDataJson.raisedExceptions];
  }
}

export default MockDataStore;
