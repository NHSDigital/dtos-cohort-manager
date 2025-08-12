import { NextResponse } from "next/server";
import { ExceptionAPIDetails } from "@/app/types/exceptionsApi";

export async function GET(request: Request) {
  const { searchParams } = new URL(request.url);
  const exceptionId = searchParams.get("exceptionId");
  const raisedOnly = searchParams.get("raisedOnly");
  const notRaisedOnly = searchParams.get("notRaisedOnly");
  const sortBy = searchParams.get("sortBy");

  // Mock data for not raised exception ID 2073
  if (exceptionId !== null && Number(exceptionId) === 2073) {
    const exception: ExceptionAPIDetails = {
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
    };
    return NextResponse.json(exception, { status: 200 });
  }

  // Mock data for not raised exception with superseded NHS number ID 2075
  if (
    (exceptionId !== null && Number(exceptionId) === 2075) ||
    Number(exceptionId) === 2085
  ) {
    const exception: ExceptionAPIDetails = {
      ExceptionId: 2073,
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
    };
    return NextResponse.json(exception, { status: 200 });
  }

  // Mock data for raised exception ID 2083
  if (exceptionId !== null && Number(exceptionId) === 2083) {
    const exception: ExceptionAPIDetails = {
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
    };
    return NextResponse.json(exception, { status: 200 });
  }

  // Mock data for not raised exception with superseded NHS number ID 2085
  if (exceptionId !== null && Number(exceptionId) === 2085) {
    const exception: ExceptionAPIDetails = {
      ExceptionId: 2073,
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
      ServiceNowCreatedDate: "",
      RecordUpdatedDate: "2025-01-17T09:27:18.25",
    };
    return NextResponse.json(exception, { status: 200 });
  }

  if (notRaisedOnly) {
    let items = [
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
        RuleDescription:
          "Another error posting the participant to the database",
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

    if (sortBy === "1") {
      items = items.sort(
        (a, b) =>
          new Date(a.DateCreated).getTime() - new Date(b.DateCreated).getTime()
      );
    } else if (sortBy === "0") {
      items = items.sort(
        (a, b) =>
          new Date(b.DateCreated).getTime() - new Date(a.DateCreated).getTime()
      );
    }

    const exceptions = {
      Items: items,
      IsFirstPage: true,
      HasNextPage: false,
      LastResultId: items[items.length - 1]?.ExceptionId ?? null,
      TotalItems: items.length,
      TotalPages: 1,
      CurrentPage: 1,
    };
    return NextResponse.json(exceptions, { status: 200 });
  }

  if (raisedOnly) {
    let items = [
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
        RuleDescription:
          "Another error posting the participant to the database",
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

    if (sortBy === "1") {
      items = items.sort(
        (a, b) =>
          new Date(a.ServiceNowCreatedDate).getTime() -
          new Date(b.ServiceNowCreatedDate).getTime()
      );
    } else if (sortBy === "0") {
      items = items.sort(
        (a, b) =>
          new Date(b.ServiceNowCreatedDate).getTime() -
          new Date(a.ServiceNowCreatedDate).getTime()
      );
    }

    const exceptions = {
      Items: items,
      IsFirstPage: true,
      HasNextPage: false,
      LastResultId: items[items.length - 1]?.ExceptionId ?? null,
      TotalItems: items.length,
      TotalPages: 1,
      CurrentPage: 1,
    };
    return NextResponse.json(exceptions, { status: 200 });
  }
}
