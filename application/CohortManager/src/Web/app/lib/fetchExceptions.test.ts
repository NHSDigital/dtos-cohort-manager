import { fetchExceptions } from "@/app/lib/fetchExceptions";
import type { ExceptionsAPI } from "@/app/types/exceptionsApi";

describe("fetchExceptions", () => {
  beforeEach(() => {
    jest.resetModules();
  });

  it("fetches exceptions from the API", async () => {
    const mockResponse: ExceptionsAPI[] = [
      {
        ExceptionId: 3670,
        NhsNumber: "9694421551",
        DateCreated: "2024-11-28T11:58:48.017",
        RuleDescription:
          "There was problem posting the participant to the database",
        DateResolved: "",
        RuleId: 0,
        Category: 0,
        ScreeningName: "",
        ExceptionDate: "",
        CohortName: "",
        Fatal: 0,
        ServiceNowId: "",
        ServiceNowCreatedDate: "",
        RecordUpdatedDate: "",
      },
      {
        ExceptionId: 3671,
        NhsNumber: "9694666112",
        DateCreated: "2024-11-28T11:58:48.393",
        RuleDescription:
          "There was an error while marking participant as eligible {eligibleResponse}",
        DateResolved: "",
        RuleId: 0,
        Category: 0,
        ScreeningName: "",
        ExceptionDate: "",
        CohortName: "",
        Fatal: 0,
        ServiceNowId: "",
        ServiceNowCreatedDate: "",
        RecordUpdatedDate: "",
      },
      {
        ExceptionId: 3676,
        NhsNumber: "9726519047",
        DateCreated: "2024-11-28T11:58:49.987",
        RuleDescription:
          "Timeout expired.  The timeout period elapsed prior to obtaining a connection from the pool.  This may have occurred because all pooled connections were in use and max pool size was reached.",
        DateResolved: "",
        RuleId: 0,
        Category: 0,
        ScreeningName: "",
        ExceptionDate: "",
        CohortName: "",
        Fatal: 0,
        ServiceNowId: "",
        ServiceNowCreatedDate: "",
        RecordUpdatedDate: "",
      },
    ];
    global.fetch = jest.fn().mockResolvedValue({
      ok: true,
      json: jest.fn().mockResolvedValue(mockResponse),
    });

    const result = await fetchExceptions();

    expect(result).toEqual(mockResponse);
  });

  it("fetches individual exception details from the API", async () => {
    const mockResponse = {
      ExceptionId: 3670,
      NhsNumber: "9694421551",
      DateCreated: "2024-11-28T11:58:48.017",
      RuleDescription:
        "There was problem posting the participant to the database",
      ExceptionDetails: {
        GivenName: "John",
        FamilyName: "Doe",
        DateOfBirth: "1993-02-26",
        ParticipantAddressLine1: "123 Fake St",
        ParticipantAddressLine2: "Fake town",
        ParticipantAddressLine3: "Fake city",
        ParticipantAddressLine4: "Fake county",
        ParticipantAddressLine5: "",
        ParticipantPostCode: "AB1 2CD",
        TelephoneNumberHome: "01234567890",
        EmailAddressHome: "john@doe.com",
        GpPracticeCode: "A12345",
      },
    };

    global.fetch = jest.fn().mockResolvedValue({
      ok: true,
      json: jest.fn().mockResolvedValue(mockResponse),
    });

    const result = await fetchExceptions(3670);

    expect(result).toEqual(mockResponse);
  });

  it("throws an error if the response is not ok", async () => {
    global.fetch = jest.fn().mockResolvedValue({
      ok: false,
      statusText: "Not found",
    });

    await expect(fetchExceptions()).rejects.toThrow(
      "Error fetching data: Not found"
    );
  });
});
