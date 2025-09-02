import { ExceptionAPIDetails } from "@/app/types/exceptionsApi";
import mockDataJson from "@/app/data/mockExceptions.json";
import { getCurrentDate } from "@/app/lib/utils";

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

// Shared in-memory data store state
let mockDataStore: {
  exceptions: Record<number, ExceptionAPIDetails>;
  notRaisedExceptions: ExceptionListItem[];
  raisedExceptions: ExceptionListItem[];
} | null = null;

// Initialize the data store with fresh data from JSON
const initializeDataStore = () => {
  mockDataStore ??= {
    exceptions: { ...mockDataJson.exceptions },
    notRaisedExceptions: [...mockDataJson.notRaisedExceptions],
    raisedExceptions: [...mockDataJson.raisedExceptions],
  };

  // Ensure certain mock exceptions always use today's date for reports
  // This keeps the sample data relevant without manual JSON edits.
  const store = mockDataStore;
  const today = getCurrentDate(); // YYYY-MM-DD

  const applyTodayDates = (
    exceptionId: number,
    createdTime: string,
    serviceNowTime: string
  ) => {
    const ex = store.exceptions[exceptionId];
    if (ex) {
      ex.DateCreated = `${today}T${createdTime}`;
      // Only set ServiceNowCreatedDate if it's a raised exception
      if (ex.ServiceNowId) {
        ex.ServiceNowCreatedDate = `${today}T${serviceNowTime}`;
      }
      ex.RecordUpdatedDate = ex.ServiceNowCreatedDate || ex.DateCreated;
    }

    // Update list mirrors
    const updateListItem = (item: typeof store.raisedExceptions[number]) => {
      if (item.ExceptionId === exceptionId) {
        item.DateCreated = `${today}T${createdTime}`;
        if (item.ServiceNowId) {
          item.ServiceNowCreatedDate = `${today}T${serviceNowTime}`;
        }
        item.RecordUpdatedDate =
          item.ServiceNowCreatedDate || item.DateCreated;
      }
    };

    store.raisedExceptions.forEach(updateListItem);
    store.notRaisedExceptions.forEach(updateListItem);
  };

  // 3001: category 12 (raised) – keep original times but set to today
  applyTodayDates(3001, "09:00:00", "10:00:00");
  // 4001: category 13 (raised) – keep original times but set to today
  applyTodayDates(4001, "09:15:00", "12:00:00");

  return mockDataStore;
};

// Helper function to create a list item from exception data
const createListItem = (exception: ExceptionAPIDetails): ExceptionListItem => ({
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
});

// Helper function to update list exceptions when ServiceNow status changes
const updateListExceptions = (
  store: typeof mockDataStore,
  exception: ExceptionAPIDetails,
  oldServiceNowId: string,
  newServiceNowId: string
): void => {
  if (!store) return;

  const wasRaised = !!oldServiceNowId;
  const isNowRaised = !!newServiceNowId;
  const listItem = createListItem(exception);

  if (wasRaised === isNowRaised) {
    // Status didn't change, just update the existing record
    if (isNowRaised) {
      // Update in raisedExceptions
      const index = store.raisedExceptions.findIndex(
        (e) => e.ExceptionId === exception.ExceptionId
      );
      if (index !== -1) {
        store.raisedExceptions[index] = listItem;
      }
    } else {
      // Update in notRaisedExceptions
      const index = store.notRaisedExceptions.findIndex(
        (e) => e.ExceptionId === exception.ExceptionId
      );
      if (index !== -1) {
        store.notRaisedExceptions[index] = listItem;
      }
    }
  } else if (isNowRaised) {
    // Move from notRaised to raised
    store.notRaisedExceptions = store.notRaisedExceptions.filter(
      (e) => e.ExceptionId !== exception.ExceptionId
    );
    store.raisedExceptions.push(listItem);
  } else {
    // Move from raised to notRaised
    store.raisedExceptions = store.raisedExceptions.filter(
      (e) => e.ExceptionId !== exception.ExceptionId
    );
    store.notRaisedExceptions.push(listItem);
  }
};

// Getter functions
export const getExceptions = (): Record<number, ExceptionAPIDetails> => {
  const store = initializeDataStore();
  return store.exceptions;
};

export const getException = (id: number): ExceptionAPIDetails | undefined => {
  const store = initializeDataStore();
  return store.exceptions[id];
};

export const getNotRaisedExceptions = (): ExceptionListItem[] => {
  const store = initializeDataStore();
  return store.notRaisedExceptions;
};

export const getRaisedExceptions = (): ExceptionListItem[] => {
  const store = initializeDataStore();
  return store.raisedExceptions;
};

// Update function
export const updateExceptionServiceNow = (
  exceptionId: number,
  serviceNowId: string
): boolean => {
  const store = initializeDataStore();
  const exception = store.exceptions[exceptionId];

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
  updateListExceptions(store, exception, oldServiceNowId, serviceNowId);

  return true;
};

// Reset function for testing
export const resetMockDataStore = (): void => {
  mockDataStore = {
    exceptions: { ...mockDataJson.exceptions },
    notRaisedExceptions: [...mockDataJson.notRaisedExceptions],
    raisedExceptions: [...mockDataJson.raisedExceptions],
  };
};

// Default export object that maintains the same interface for compatibility
const mockDataStoreApi = {
  getExceptions,
  getException,
  getNotRaisedExceptions,
  getRaisedExceptions,
  updateExceptionServiceNow,
  reset: resetMockDataStore,
};

export default mockDataStoreApi;
