import { NextResponse } from "next/server";
import { ExceptionAPIDetails } from "@/app/types/exceptionsApi";

// Mock data store
const mockExceptions: Record<number, ExceptionAPIDetails> = {
  2073: {
    ExceptionId: 2073,
    NhsNumber: "1211111881",
    DateCreated: "2025-05-16T14:26:09.28",
    DateResolved: "9999-12-31T00:00:00",
    RuleId: -2146233088,
    RuleDescription:
      "There was problem posting the participant to the database",
    Category: 5,
    ScreeningName: "Breast Screening",
    ExceptionDate: "2025-01-15T00:00:00",
    CohortName: "",
    Fatal: 1,
    ExceptionDetails: {
      GivenName: "Alice",
      FamilyName: "Smith",
      DateOfBirth: "19800101",
      ParticipantAddressLine1: "123 Main Street",
      ParticipantAddressLine2: "Flat 2B",
      ParticipantAddressLine3: "Central District",
      ParticipantAddressLine4: "London",
      ParticipantAddressLine5: "England",
      ParticipantPostCode: "E1 6AN",
      TelephoneNumberHome: "07123456789",
      EmailAddressHome: "alice.smith@example.com",
      PrimaryCareProvider: "E12345",
      Gender: 2,
      SupersededByNhsNumber: "",
    },
    ServiceNowId: "",
    ServiceNowCreatedDate: "",
    RecordUpdatedDate: "2025-01-17T09:27:18.25",
  },
  2075: {
    ExceptionId: 2075,
    NhsNumber: "1211111881",
    DateCreated: "2025-05-16T14:26:09.28",
    DateResolved: "9999-12-31T00:00:00",
    RuleId: 21,
    RuleDescription: "The 'Superseded by NHS number' field is not null",
    Category: 5,
    ScreeningName: "Breast Screening",
    ExceptionDate: "2025-01-15T00:00:00",
    CohortName: "",
    Fatal: 1,
    ExceptionDetails: {
      GivenName: "Alice",
      FamilyName: "Smith",
      DateOfBirth: "19800101",
      ParticipantAddressLine1: "123 Main Street",
      ParticipantAddressLine2: "Flat 2B",
      ParticipantAddressLine3: "Central District",
      ParticipantAddressLine4: "London",
      ParticipantAddressLine5: "England",
      ParticipantPostCode: "E1 6AN",
      TelephoneNumberHome: "07123456789",
      EmailAddressHome: "alice.smith@example.com",
      PrimaryCareProvider: "E12345",
      Gender: 2,
      SupersededByNhsNumber: "1211111882",
    },
    ServiceNowId: "",
    ServiceNowCreatedDate: "",
    RecordUpdatedDate: "2025-01-17T09:27:18.25",
  },
  2083: {
    ExceptionId: 2083,
    NhsNumber: "1211111881",
    DateCreated: "2025-05-16T14:26:09.28",
    DateResolved: "9999-12-31T00:00:00",
    RuleId: -2146233088,
    RuleDescription:
      "There was problem posting the participant to the database",
    Category: 5,
    ScreeningName: "Breast Screening",
    ExceptionDate: "2025-01-15T00:00:00",
    CohortName: "",
    Fatal: 1,
    ExceptionDetails: {
      GivenName: "Bob",
      FamilyName: "Johnson",
      DateOfBirth: "19751212",
      ParticipantAddressLine1: "456 Oak Avenue",
      ParticipantAddressLine2: "",
      ParticipantAddressLine3: "West End",
      ParticipantAddressLine4: "Manchester",
      ParticipantAddressLine5: "UK",
      ParticipantPostCode: "M1 2AB",
      TelephoneNumberHome: "07234567890",
      EmailAddressHome: "bob.johnson@example.com",
      PrimaryCareProvider: "E54321",
      Gender: 1,
      SupersededByNhsNumber: "",
    },
    ServiceNowId: "INC0002765",
    ServiceNowCreatedDate: "2025-06-16T00:00:00",
    RecordUpdatedDate: "2025-01-17T09:27:18.25",
  },
  2085: {
    ExceptionId: 2085,
    NhsNumber: "1211111881",
    DateCreated: "2025-05-16T14:26:09.28",
    DateResolved: "9999-12-31T00:00:00",
    RuleId: 21,
    RuleDescription: "The 'Superseded by NHS number' field is not null",
    Category: 5,
    ScreeningName: "Breast Screening",
    ExceptionDate: "2025-01-15T00:00:00",
    CohortName: "",
    Fatal: 1,
    ExceptionDetails: {
      GivenName: "Alice",
      FamilyName: "Smith",
      DateOfBirth: "19800101",
      ParticipantAddressLine1: "123 Main Street",
      ParticipantAddressLine2: "Flat 2B",
      ParticipantAddressLine3: "Central District",
      ParticipantAddressLine4: "London",
      ParticipantAddressLine5: "England",
      ParticipantPostCode: "E1 6AN",
      TelephoneNumberHome: "07123456789",
      EmailAddressHome: "alice.smith@example.com",
      PrimaryCareProvider: "E12345",
      Gender: 2,
      SupersededByNhsNumber: "1211111882",
    },
    ServiceNowId: "INC0002768",
    ServiceNowCreatedDate: "2025-06-16T00:00:00",
    RecordUpdatedDate: "2025-01-17T09:27:18.25",
  },
};

// List data for different exception types
const notRaisedExceptions = [
  {
    ExceptionId: 2073,
    NhsNumber: "1211111881",
    DateCreated: "2025-05-16T14:26:09.28",
    DateResolved: "9999-12-31T00:00:00",
    RuleId: -2146233088,
    RuleDescription:
      "There was problem posting the participant to the database",
    Category: 5,
    ScreeningName: "Breast Screening",
    ExceptionDate: "2025-01-15T00:00:00",
    CohortName: "",
    Fatal: 1,
    ServiceNowId: "",
    ServiceNowCreatedDate: "",
    RecordUpdatedDate: "2025-01-17T09:27:18.25",
  },
  {
    ExceptionId: 2074,
    NhsNumber: "1211111882",
    DateCreated: "2025-05-17T10:00:00.00",
    DateResolved: "9999-12-31T00:00:00",
    RuleId: -2146233089,
    RuleDescription: "Another error posting the participant to the database",
    Category: 5,
    ScreeningName: "Breast Screening",
    ExceptionDate: "2025-01-16T00:00:00",
    CohortName: "",
    Fatal: 1,
    ServiceNowId: "",
    ServiceNowCreatedDate: "",
    RecordUpdatedDate: "2025-01-18T09:27:18.25",
  },
  {
    ExceptionId: 2075,
    NhsNumber: "1211111881",
    DateCreated: "2025-05-16T14:26:09.28",
    DateResolved: "9999-12-31T00:00:00",
    RuleId: 21,
    RuleDescription: "The 'Superseded by NHS number' field is not null",
    Category: 5,
    ScreeningName: "Breast Screening",
    ExceptionDate: "2025-01-15T00:00:00",
    CohortName: "",
    Fatal: 1,
    ServiceNowId: "",
    ServiceNowCreatedDate: "",
    RecordUpdatedDate: "2025-01-17T09:27:18.25",
  },
];

const raisedExceptions = [
  {
    ExceptionId: 2083,
    NhsNumber: "1211111881",
    DateCreated: "2025-05-16T14:26:09.28",
    DateResolved: "9999-12-31T00:00:00",
    RuleId: -2146233088,
    RuleDescription:
      "There was problem posting the participant to the database",
    Category: 5,
    ScreeningName: "Breast Screening",
    ExceptionDate: "2025-01-15T00:00:00",
    CohortName: "",
    Fatal: 1,
    ServiceNowId: "INC0002764",
    ServiceNowCreatedDate: "2025-06-16:00:00",
    RecordUpdatedDate: "2025-01-17T09:27:18.25",
  },
  {
    ExceptionId: 2084,
    NhsNumber: "1211111882",
    DateCreated: "2025-05-17T10:00:00.00",
    DateResolved: "9999-12-31T00:00:00",
    RuleId: -2146233089,
    RuleDescription: "Another error posting the participant to the database",
    Category: 5,
    ScreeningName: "Breast Screening",
    ExceptionDate: "2025-01-16T00:00:00",
    CohortName: "",
    Fatal: 1,
    ServiceNowId: "INC0002765",
    ServiceNowCreatedDate: "2025-06-10T00:00:00",
    RecordUpdatedDate: "2025-01-18T09:27:18.25",
  },
  {
    ExceptionId: 2085,
    NhsNumber: "1211111883",
    DateCreated: "2025-05-18T11:00:00.00",
    DateResolved: "9999-12-31T00:00:00",
    RuleId: 21,
    RuleDescription: "The 'Superseded by NHS number' field is not null",
    Category: 5,
    ScreeningName: "Breast Screening",
    ExceptionDate: "2025-01-17T00:00:00",
    CohortName: "",
    Fatal: 1,
    ServiceNowId: "INC0002766",
    ServiceNowCreatedDate: "2025-06-12T00:00:00",
    RecordUpdatedDate: "2025-01-19T09:27:18.25",
  },
];

// Helper functions
function sortExceptions<
  T extends { DateCreated: string; ServiceNowCreatedDate?: string }
>(items: T[], sortBy: string | null, dateField: keyof T = "DateCreated"): T[] {
  if (sortBy === "1") {
    return items.sort(
      (a, b) =>
        new Date(a[dateField] as string).getTime() -
        new Date(b[dateField] as string).getTime()
    );
  } else if (sortBy === "0") {
    return items.sort(
      (a, b) =>
        new Date(b[dateField] as string).getTime() -
        new Date(a[dateField] as string).getTime()
    );
  }
  return items;
}

function createExceptionListResponse<T extends { ExceptionId: number }>(
  items: T[]
) {
  return {
    Items: items,
    IsFirstPage: true,
    HasNextPage: false,
    LastResultId: items[items.length - 1]?.ExceptionId ?? null,
    TotalItems: items.length,
    TotalPages: 1,
    CurrentPage: 1,
  };
}

export async function GET(request: Request) {
  const { searchParams } = new URL(request.url);
  const exceptionId = searchParams.get("exceptionId");
  const raisedOnly = searchParams.get("raisedOnly");
  const notRaisedOnly = searchParams.get("notRaisedOnly");
  const sortBy = searchParams.get("sortBy");

  // Handle single exception requests
  if (exceptionId !== null) {
    const id = Number(exceptionId);
    const exception = mockExceptions[id];

    if (exception) {
      return NextResponse.json(exception, { status: 200 });
    }

    return NextResponse.json(
      { error: `Exception with ID ${exceptionId} not found` },
      { status: 404 }
    );
  }

  // Handle list requests
  if (notRaisedOnly) {
    const sortedItems = sortExceptions([...notRaisedExceptions], sortBy);
    const response = createExceptionListResponse(sortedItems);
    return NextResponse.json(response, { status: 200 });
  }

  if (raisedOnly) {
    const sortedItems = sortExceptions(
      [...raisedExceptions],
      sortBy,
      "ServiceNowCreatedDate"
    );
    const response = createExceptionListResponse(sortedItems);
    return NextResponse.json(response, { status: 200 });
  }

  // Default fallback
  return NextResponse.json(
    { error: "No valid query parameters provided" },
    { status: 400 }
  );
}
