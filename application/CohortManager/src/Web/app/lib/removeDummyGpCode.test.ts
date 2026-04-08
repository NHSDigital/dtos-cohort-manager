class NextRedirectError extends Error {
  constructor(public url: string) {
    super(`NEXT_REDIRECT;${url}`);
    this.name = "NextRedirectError";
  }
}

jest.mock("next/navigation", () => ({
  redirect: jest.fn().mockImplementation((url: string) => {
    throw new NextRedirectError(url);
  }),
}));

const mockSafeParse = jest.fn();
jest.mock("./formValidationSchemas", () => {
  return {
    get removeDummyGpCodeSchema() {
      return { safeParse: mockSafeParse };
    },
  };
});

import { removeDummyGpCode } from "./removeDummyGpCode";
import { redirect } from "next/navigation";

const mockRedirect = redirect as jest.MockedFunction<typeof redirect>;

describe("removeDummyGpCode", () => {
  beforeEach(() => {
    jest.clearAllMocks();
    process.env.REMOVE_DUMMY_GP_CODE_API_URL = "https://api.example.com";
    global.fetch = jest.fn();
  });

  afterEach(() => {
    delete process.env.REMOVE_DUMMY_GP_CODE_API_URL;
  });

  function createFormData(overrides: Record<string, string> = {}): FormData {
    const formData = new FormData();
    formData.append("nhsNumber", overrides.nhsNumber ?? "1234567890");
    formData.append("forename", overrides.forename ?? "Jane");
    formData.append("surname", overrides.surname ?? "Smith");
    formData.append("dob-day", overrides["dob-day"] ?? "15");
    formData.append("dob-month", overrides["dob-month"] ?? "01");
    formData.append("dob-year", overrides["dob-year"] ?? "1990");
    formData.append("serviceNowTicketNumber", overrides.serviceNowTicketNumber ?? "CS1234567");
    return formData;
  }

  describe("validation errors", () => {
    it("returns error state when NHS Number is invalid", async () => {
      mockSafeParse.mockReturnValue({
        success: false,
        error: {
          issues: [{ message: "NHS Number must be 10 digits", path: ["nhsNumber"] }],
        },
      });

      const formData = createFormData({ nhsNumber: "123" });
      const result = await removeDummyGpCode(null, formData);

      expect(result).toEqual({
        error: "NHS Number must be 10 digits",
        field: "nhsNumber",
        values: {
          nhsNumber: "123",
          forename: "Jane",
          surname: "Smith",
          dobDay: "15",
          dobMonth: "01",
          dobYear: "1990",
          serviceNowTicketNumber: "CS1234567",
        },
      });
      expect(mockRedirect).not.toHaveBeenCalled();
    });

    it("returns error state when forename is empty", async () => {
      mockSafeParse.mockReturnValue({
        success: false,
        error: {
          issues: [{ message: "Forename is required", path: ["forename"] }],
        },
      });

      const formData = createFormData({ forename: "" });
      const result = await removeDummyGpCode(null, formData);

      expect(result).toEqual({
        error: "Forename is required",
        field: "forename",
        values: expect.objectContaining({ forename: "" }),
      });
      expect(mockRedirect).not.toHaveBeenCalled();
    });

    it("returns error state anchored to dob-day when date of birth is invalid", async () => {
      mockSafeParse.mockReturnValue({
        success: false,
        error: {
          issues: [{ message: "Date of Birth is required", path: ["dateOfBirth"] }],
        },
      });

      const formData = createFormData({ "dob-day": "", "dob-month": "", "dob-year": "" });
      const result = await removeDummyGpCode(null, formData);

      expect(result).toEqual({
        error: "Date of Birth is required",
        field: "dob-day",
        values: expect.objectContaining({ dobDay: "", dobMonth: "", dobYear: "" }),
      });
      expect(mockRedirect).not.toHaveBeenCalled();
    });

    it("returns error state anchored to serviceNowTicketNumber", async () => {
      mockSafeParse.mockReturnValue({
        success: false,
        error: {
          issues: [{ message: "Service Now Ticket Number is required", path: ["serviceNowTicketNumber"] }],
        },
      });

      const formData = createFormData({ serviceNowTicketNumber: "" });
      const result = await removeDummyGpCode(null, formData);

      expect(result).toEqual({
        error: "Service Now Ticket Number is required",
        field: "serviceNowTicketNumber",
        values: expect.objectContaining({ serviceNowTicketNumber: "" }),
      });
      expect(mockRedirect).not.toHaveBeenCalled();
    });
  });

  describe("successful API call (202 Accepted)", () => {
    it("redirects with success=true on 202 response", async () => {
      mockSafeParse.mockReturnValue({
        success: true,
        data: {
          nhsNumber: "1234567890",
          forename: "Jane",
          surname: "Smith",
          dateOfBirth: "1990-01-15",
          serviceNowTicketNumber: "CS1234567",
        },
      });

      global.fetch = jest.fn().mockResolvedValue({
        status: 202,
      });

      const formData = createFormData();

      await expect(removeDummyGpCode(null, formData)).rejects.toThrow(
        "NEXT_REDIRECT;/remove-dummy-gp-code?success=true"
      );

      expect(fetch).toHaveBeenCalledWith(
        "https://api.example.com/api/RemoveDummyGPCode",
        {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
          },
          body: JSON.stringify({
            nhs_number: "1234567890",
            forename: "Jane",
            surname: "Smith",
            date_of_birth: "1990-01-15",
            request_id: "CS1234567",
          }),
        }
      );

      expect(mockRedirect).toHaveBeenCalledWith("/remove-dummy-gp-code?success=true");
    });
  });

  describe("API error handling (400 Bad Request)", () => {
    it("returns error state with 'Participant Not Found' on 400 response", async () => {
      mockSafeParse.mockReturnValue({
        success: true,
        data: {
          nhsNumber: "1234567890",
          forename: "Jane",
          surname: "Smith",
          dateOfBirth: "1990-01-15",
          serviceNowTicketNumber: "CS1234567",
        },
      });

      global.fetch = jest.fn().mockResolvedValue({
        status: 400,
      });

      const formData = createFormData();
      const result = await removeDummyGpCode(null, formData);

      expect(result).toEqual({
        error: "The participant could not be found or the details provided do not match",
        values: expect.objectContaining({ nhsNumber: "1234567890" }),
      });
    });
  });

  describe("unexpected errors", () => {
    it("returns error state with generic message on non-202/non-400 response", async () => {
      mockSafeParse.mockReturnValue({
        success: true,
        data: {
          nhsNumber: "1234567890",
          forename: "Jane",
          surname: "Smith",
          dateOfBirth: "1990-01-15",
          serviceNowTicketNumber: "CS1234567",
        },
      });

      global.fetch = jest.fn().mockResolvedValue({
        status: 500,
      });

      const formData = createFormData();
      const result = await removeDummyGpCode(null, formData);

      expect(result).toEqual({
        error: "An unexpected error occurred. Please try again later.",
        values: expect.objectContaining({ nhsNumber: "1234567890" }),
      });
    });

    it("returns connection error when fetch throws a network error", async () => {
      mockSafeParse.mockReturnValue({
        success: true,
        data: {
          nhsNumber: "1234567890",
          forename: "Jane",
          surname: "Smith",
          dateOfBirth: "1990-01-15",
          serviceNowTicketNumber: "CS1234567",
        },
      });

      global.fetch = jest.fn().mockRejectedValue(new Error("fetch failed"));

      const formData = createFormData();
      const result = await removeDummyGpCode(null, formData);

      expect(result).toEqual({
        error: "Unable to connect to the service. Please try again later.",
        values: expect.objectContaining({ nhsNumber: "1234567890" }),
      });
    });
  });

  describe("form value preservation on error", () => {
    it("returns all submitted values when validation fails so the form can restore them", async () => {
      mockSafeParse.mockReturnValue({
        success: false,
        error: {
          issues: [{ message: "Invalid NHS Number", path: ["nhsNumber"] }],
        },
      });

      const formData = createFormData({
        nhsNumber: "invalid",
        forename: "John",
        surname: "Doe",
        "dob-day": "25",
        "dob-month": "12",
        "dob-year": "1985",
        serviceNowTicketNumber: "CS9999999",
      });
      const result = await removeDummyGpCode(null, formData);

      expect(result?.values).toEqual({
        nhsNumber: "invalid",
        forename: "John",
        surname: "Doe",
        dobDay: "25",
        dobMonth: "12",
        dobYear: "1985",
        serviceNowTicketNumber: "CS9999999",
      });
    });
  });

  describe("date of birth assembly", () => {
    it("assembles date of birth from day, month, year fields", async () => {
      mockSafeParse.mockReturnValue({
        success: true,
        data: {
          nhsNumber: "1234567890",
          forename: "Jane",
          surname: "Smith",
          dateOfBirth: "1990-03-05",
          serviceNowTicketNumber: "CS1234567",
        },
      });

      global.fetch = jest.fn().mockResolvedValue({
        status: 202,
      });

      const formData = createFormData({
        "dob-day": "5",
        "dob-month": "3",
        "dob-year": "1990",
      });

      await expect(removeDummyGpCode(null, formData)).rejects.toThrow(
        "NEXT_REDIRECT;/remove-dummy-gp-code?success=true"
      );

      expect(mockSafeParse).toHaveBeenCalledWith(
        expect.objectContaining({
          dateOfBirth: "1990-03-05",
        })
      );
    });
  });
});
