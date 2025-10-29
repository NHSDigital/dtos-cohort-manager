import { fetchReports } from "@/app/lib/fetchReports";

describe("fetchReports", () => {
  const ORIGINAL_ENV = process.env.EXCEPTIONS_API_URL;

  beforeEach(() => {
    jest.resetModules();
    process.env.EXCEPTIONS_API_URL = "https://example.test";
  });

  afterEach(() => {
    process.env.EXCEPTIONS_API_URL = ORIGINAL_ENV;
    // @ts-expect-error - reset mocked global fetch
    global.fetch = undefined;
  });

  it("fetches reports from the API with correct query params", async () => {
    const category = 12;
    const date = "2025-08-27";
    const expectedUrl =
      `${process.env.EXCEPTIONS_API_URL}` +
      `/api/GetValidationExceptions?exceptionStatus=1&sortOrder=1&exceptionCategory=${category}&isReport=1&reportDate=${date}`;

    const mockResponse = {
      Items: [
        {
          ExceptionId: 1,
          NhsNumber: "1234567890",
          DateCreated: "2025-08-27T10:00:00.000Z",
          DateResolved: "",
          RuleId: 0,
          RuleDescription: "Possible Confusion",
          Category: category,
          ScreeningName: "",
          ExceptionDate: "2025-08-27T10:00:00.000Z",
          CohortName: "",
          Fatal: 0,
          ServiceNowId: "",
          ServiceNowCreatedDate: "",
          RecordUpdatedDate: "",
          ExceptionDetails: {
            GivenName: "Jane",
            FamilyName: "Doe",
            DateOfBirth: "1990-01-01",
            Gender: 1,
            ParticipantAddressLine1: "",
            ParticipantAddressLine2: "",
            ParticipantAddressLine3: "",
            ParticipantAddressLine4: "",
            ParticipantAddressLine5: "",
            ParticipantPostCode: "",
            TelephoneNumberHome: "",
            EmailAddressHome: "",
            PrimaryCareProvider: "",
          },
        },
      ],
    };

    global.fetch = jest.fn().mockResolvedValue({
      ok: true,
      json: jest.fn().mockResolvedValue(mockResponse),
    });

    const result = await fetchReports(category, date);

    expect(global.fetch).toHaveBeenCalledWith(expectedUrl);
    expect(result).toEqual(mockResponse);
  });

  it("throws an error if the response is not ok", async () => {
    const category = 13;
    const date = "2025-08-26";
    const expectedUrl =
      `${process.env.EXCEPTIONS_API_URL}` +
      `/api/GetValidationExceptions?exceptionStatus=1&sortOrder=1&exceptionCategory=${category}&isReport=1&reportDate=${date}`;

    global.fetch = jest.fn().mockResolvedValue({
      ok: false,
      status: 404,
      statusText: "Not Found",
    });

    await expect(fetchReports(category, date)).rejects.toThrow(
      `Error fetching data for reports: 404 Not Found from ${expectedUrl}`
    );
  });
});
