import { fetchExceptions } from "@/app/lib/fetchExceptions";
import { auth } from "@/app/lib/auth";
import type { ExceptionsAPI } from "@/app/types/exceptionsApi";
import type { Session } from "next-auth";

jest.mock("./auth", () => ({
  auth: jest.fn(),
}));

const mockAuth = auth as unknown as jest.MockedFunction<
  () => Promise<Session | null>
>;

describe("fetchExceptions", () => {
  beforeEach(() => {
    jest.resetModules();
    mockAuth.mockResolvedValue(null);
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
    globalThis.fetch = jest.fn().mockResolvedValue({
      ok: true,
      json: jest.fn().mockResolvedValue(mockResponse),
      headers: { get: jest.fn().mockReturnValue(null) },
    });

    const result = await fetchExceptions();
    expect(result.data).toEqual(mockResponse);
    expect(globalThis.fetch).toHaveBeenCalledWith(
      expect.stringContaining("/api/GetValidationExceptions?"),
      expect.objectContaining({
        cache: "no-store",
        headers: undefined,
      })
    );
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

    globalThis.fetch = jest.fn().mockResolvedValue({
      ok: true,
      json: jest.fn().mockResolvedValue(mockResponse),
      headers: { get: jest.fn().mockReturnValue(null) },
    });

    const result = await fetchExceptions({ exceptionId: 3670 });
    expect(result.data).toEqual(mockResponse);
  });

  it("throws an error if the response is not ok", async () => {
    globalThis.fetch = jest.fn().mockResolvedValue({
      ok: false,
      statusText: "Not found",
    });

    await expect(fetchExceptions()).rejects.toThrow(
      "Error fetching data: Not found"
    );
  });

  it("adds the JWT token as an authorization header when available", async () => {
    mockAuth.mockResolvedValue({
      expires: new Date(Date.now() + 60_000).toISOString(),
      idToken: "dev-jwt-token",
      user: {
        name: "Test User",
        email: null,
        image: null,
        uid: "testuid",
      },
    });

    globalThis.fetch = jest.fn().mockResolvedValue({
      ok: true,
      json: jest.fn().mockResolvedValue([]),
      headers: { get: jest.fn().mockReturnValue(null) },
    });

    await fetchExceptions();

    expect(globalThis.fetch).toHaveBeenCalledWith(
      expect.stringContaining("/api/GetValidationExceptions?"),
      expect.objectContaining({
        cache: "no-store",
        headers: {
          Authorization: "Bearer dev-jwt-token",
        },
      })
    );
  });
});
