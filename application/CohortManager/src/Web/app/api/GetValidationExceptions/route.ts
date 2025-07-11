import { NextResponse } from "next/server";
import { ExceptionAPIDetails } from "@/app/types/exceptionsApi";

export async function GET(request: Request) {
  const { searchParams } = new URL(request.url);
  const exceptionId = searchParams.get("exceptionId");
  const raisedOnly = searchParams.get("raisedOnly");
  const notRaisedOnly = searchParams.get("notRaisedOnly");

  if (exceptionId !== null && Number(exceptionId) === 2073) {
    const exception: ExceptionAPIDetails = {
      ExceptionId: 2073,
      FileName:
        "202411261720028838554_1B8F53_-_CAAS_BREAST_SCREENING_COHORT.parquet",
      NhsNumber: "1211111881",
      DateCreated: "2025-05-16T14:26:09.28",
      DateResolved: "9999-12-31T00:00:00",
      RuleId: -2146233088,
      RuleDescription:
        "There was problem posting the participant to the database",
      ErrorRecord:
        '{"RecordType":"ADD","NhsNumber":"1211111881","RemovalReason":"","RemovalEffectiveFromDate":"","ScreeningId":"1","ScreeningName":"Breast Screening","EligibilityFlag":"1"}',
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
      },
      ServiceNowId: "",
      ServiceNowCreatedDate: "",
      RecordUpdatedDate: "2025-01-17T09:27:18.25",
    };
    return NextResponse.json(exception, { status: 200 });
  }

  if (exceptionId !== null && Number(exceptionId) === 2083) {
    const exception: ExceptionAPIDetails = {
      ExceptionId: 2083,
      FileName:
        "202411261720028838554_1B8F53_-_CAAS_BREAST_SCREENING_COHORT.parquet",
      NhsNumber: "1211111881",
      DateCreated: "2025-05-16T14:26:09.28",
      DateResolved: "9999-12-31T00:00:00",
      RuleId: -2146233088,
      RuleDescription:
        "There was problem posting the participant to the database",
      ErrorRecord:
        '{"RecordType":"ADD","NhsNumber":"1211111881","RemovalReason":"","RemovalEffectiveFromDate":"","ScreeningId":"1","ScreeningName":"Breast Screening","EligibilityFlag":"1"}',
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
      },
      ServiceNowId: "INC0002765",
      ServiceNowCreatedDate: "2025-06-16T00:00:00",
      RecordUpdatedDate: "2025-01-17T09:27:18.25",
    };
    return NextResponse.json(exception, { status: 200 });
  }

  if (notRaisedOnly) {
    const exceptions = {
      Items: [
        {
          ExceptionId: 2073,
          FileName:
            "202411261720028838554_1B8F53_-_CAAS_BREAST_SCREENING_COHORT.parquet",
          NhsNumber: "1211111881",
          DateCreated: "2025-05-16T14:26:09.28",
          DateResolved: "9999-12-31T00:00:00",
          RuleId: -2146233088,
          RuleDescription:
            "There was problem posting the participant to the database",
          ErrorRecord:
            '{"RecordType":"ADD","NhsNumber":"1211111881","RemovalReason":"","RemovalEffectiveFromDate":"","ScreeningId":"1","ScreeningName":"Breast Screening","EligibilityFlag":"1"}',
          Category: 5,
          ScreeningName: "Breast Screening",
          ExceptionDate: "2025-01-15T00:00:00",
          CohortName: "",
          Fatal: 1,
          ExceptionDetails: {
            GivenName: "Charlie",
            FamilyName: "Williams",
            DateOfBirth: "19900228",
            ParticipantAddressLine1: "789 Pine Road",
            ParticipantAddressLine2: "Suite 5",
            ParticipantAddressLine3: "East Side",
            ParticipantAddressLine4: "Birmingham",
            ParticipantAddressLine5: "United Kingdom",
            ParticipantPostCode: "B2 4QA",
            TelephoneNumberHome: "07345678901",
            EmailAddressHome: "charlie.williams@example.com",
            PrimaryCareProvider: "E67890",
            Gender: 1,
          },
          ServiceNowId: "",
          ServiceNowCreatedDate: "",
          RecordUpdatedDate: "2025-01-17T09:27:18.25",
        },
        {
          ExceptionId: 2074,
          FileName:
            "202411261720028838554_1B8F53_-_CAAS_BREAST_SCREENING_COHORT.parquet",
          NhsNumber: "1211111882",
          DateCreated: "2025-05-17T10:00:00.00",
          DateResolved: "9999-12-31T00:00:00",
          RuleId: -2146233089,
          RuleDescription:
            "Another error posting the participant to the database",
          ErrorRecord:
            '{"RecordType":"ADD","NhsNumber":"1211111882","RemovalReason":"","RemovalEffectiveFromDate":"","ScreeningId":"1","ScreeningName":"Breast Screening","EligibilityFlag":"1"}',
          Category: 5,
          ScreeningName: "Breast Screening",
          ExceptionDate: "2025-01-16T00:00:00",
          CohortName: "",
          Fatal: 1,
          ExceptionDetails: {
            GivenName: "Diana",
            FamilyName: "Brown",
            DateOfBirth: "19851130",
            ParticipantAddressLine1: "321 Maple Lane",
            ParticipantAddressLine2: "",
            ParticipantAddressLine3: "Northfield",
            ParticipantAddressLine4: "Leeds",
            ParticipantAddressLine5: "England",
            ParticipantPostCode: "LS1 4DT",
            TelephoneNumberHome: "07456789012",
            EmailAddressHome: "diana.brown@example.com",
            PrimaryCareProvider: "E98765",
            Gender: 2,
          },
          ServiceNowId: "",
          ServiceNowCreatedDate: "",
          RecordUpdatedDate: "2025-01-18T09:27:18.25",
        },
      ],
      IsFirstPage: true,
      HasNextPage: false,
      LastResultId: 2074,
      TotalItems: 2,
      TotalPages: 1,
      CurrentPage: 1,
    };
    return NextResponse.json(exceptions, { status: 200 });
  }

  if (raisedOnly) {
    const exceptions = {
      Items: [
        {
          ExceptionId: 2083,
          FileName:
            "202411261720028838554_1B8F53_-_CAAS_BREAST_SCREENING_COHORT.parquet",
          NhsNumber: "1211111881",
          DateCreated: "2025-05-16T14:26:09.28",
          DateResolved: "9999-12-31T00:00:00",
          RuleId: -2146233088,
          RuleDescription:
            "There was problem posting the participant to the database",
          ErrorRecord:
            '{"RecordType":"ADD","NhsNumber":"1211111881","RemovalReason":"","RemovalEffectiveFromDate":"","ScreeningId":"1","ScreeningName":"Breast Screening","EligibilityFlag":"1"}',
          Category: 5,
          ScreeningName: "Breast Screening",
          ExceptionDate: "2025-01-15T00:00:00",
          CohortName: "",
          Fatal: 1,
          ExceptionDetails: {
            GivenName: "Eve",
            FamilyName: "Davis",
            DateOfBirth: "19730315",
            ParticipantAddressLine1: "654 Willow Way",
            ParticipantAddressLine2: "Apt 10",
            ParticipantAddressLine3: "South Park",
            ParticipantAddressLine4: "Liverpool",
            ParticipantAddressLine5: "UK",
            ParticipantPostCode: "L1 8JQ",
            TelephoneNumberHome: "07567890123",
            EmailAddressHome: "eve.davis@example.com",
            PrimaryCareProvider: "E24680",
            Gender: 2,
          },
          ServiceNowId: "INC0002765",
          ServiceNowCreatedDate: "2025-06-16T00:00:00",
          RecordUpdatedDate: "2025-01-17T09:27:18.25",
        },
      ],
      IsFirstPage: true,
      HasNextPage: false,
      LastResultId: 2083,
      TotalItems: 1,
      TotalPages: 1,
      CurrentPage: 1,
    };
    return NextResponse.json(exceptions, { status: 200 });
  }
}
