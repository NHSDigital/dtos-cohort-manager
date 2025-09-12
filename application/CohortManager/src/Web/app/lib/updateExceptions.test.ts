// Mock the redirect function to throw (like real Next.js redirect)
// Next.js redirect() throws an error to halt execution - this is expected behavior
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

// Mock the validation schema with a simple jest.fn()
const mockSafeParse = jest.fn();
jest.mock("./formValidationSchemas", () => ({
  updateExceptionsSchema: jest.fn(() => ({
    safeParse: mockSafeParse,
  })),
}));

import { updateExceptions } from "./updateExceptions";
import { redirect } from "next/navigation";

const mockRedirect = redirect as jest.MockedFunction<typeof redirect>;

describe("updateExceptions", () => {
  beforeEach(() => {
    jest.clearAllMocks();
    process.env.EXCEPTIONS_API_URL = "https://api.example.com";

    // Mock global fetch for all tests
    global.fetch = jest.fn();
  });

  afterEach(() => {
    delete process.env.EXCEPTIONS_API_URL;
  });

  describe("validation errors", () => {
    it("redirects with validation error when ServiceNow ID is invalid", async () => {
      mockSafeParse.mockReturnValue({
        success: false,
        error: {
          issues: [
            { message: "ServiceNow ID must be at least 9 characters long" },
          ],
        },
      });

      const formData = new FormData();
      formData.append("serviceNowID", "SHORT");
      formData.append("isEditMode", "false");

      await expect(updateExceptions(2073, formData)).rejects.toThrow(
        new NextRedirectError(
          "/participant-information/2073?error=ServiceNow%20ID%20must%20be%20at%20least%209%20characters%20long"
        )
      );

      expect(mockRedirect).toHaveBeenCalledWith(
        "/participant-information/2073?error=ServiceNow%20ID%20must%20be%20at%20least%209%20characters%20long"
      );
    });

    it("redirects with validation error in edit mode", async () => {
      mockSafeParse.mockReturnValue({
        success: false,
        error: {
          issues: [{ message: "ServiceNow ID cannot contain spaces" }],
        },
      });

      const formData = new FormData();
      formData.append("serviceNowID", "INC 123456789");
      formData.append("isEditMode", "true");

      await expect(updateExceptions(2073, formData)).rejects.toThrow(
        "NEXT_REDIRECT;/participant-information/2073?edit=true&error=ServiceNow%20ID%20cannot%20contain%20spaces"
      );

      expect(mockRedirect).toHaveBeenCalledWith(
        "/participant-information/2073?edit=true&error=ServiceNow%20ID%20cannot%20contain%20spaces"
      );
    });

    it("handles validation error when no error message is available", async () => {
      mockSafeParse.mockReturnValue({
        success: false,
        error: {
          issues: [],
        },
      });

      const formData = new FormData();
      formData.append("serviceNowID", "");
      formData.append("isEditMode", "false");

      await expect(updateExceptions(2073, formData)).rejects.toThrow(
        "NEXT_REDIRECT;/participant-information/2073?error=undefined"
      );

      expect(mockRedirect).toHaveBeenCalledWith(
        "/participant-information/2073?error=undefined"
      );
    });
  });

  describe("successful API calls", () => {
    it("successfully updates ServiceNow ID and redirects", async () => {
      mockSafeParse.mockReturnValue({
        success: true,
        data: { serviceNowID: "INC1234567890" },
      });

      global.fetch = jest.fn().mockResolvedValue({
        ok: true,
        json: jest.fn().mockResolvedValue({
          message:
            "Exception record updated with ServiceNow number successfully",
          exceptionId: 2073,
          previousServiceNowId: "",
          newServiceNowId: "INC1234567890",
        }),
      });

      const formData = new FormData();
      formData.append("serviceNowID", "INC1234567890");
      formData.append("isEditMode", "false");

      await expect(updateExceptions(2073, formData)).rejects.toThrow(
        "NEXT_REDIRECT;/exceptions"
      );

      expect(fetch).toHaveBeenCalledWith(
        "https://api.example.com/api/UpdateExceptionServiceNowId",
        {
          method: "PUT",
          headers: {
            "Content-Type": "application/json",
          },
          body: JSON.stringify({
            ExceptionId: 2073,
            ServiceNowId: "INC1234567890",
          }),
        }
      );

      expect(mockRedirect).toHaveBeenCalledWith("/exceptions");
    });

    it("handles empty ServiceNow ID (clearing the field)", async () => {
      mockSafeParse.mockReturnValue({
        success: true,
        data: { serviceNowID: "" },
      });

      global.fetch = jest.fn().mockResolvedValue({
        ok: true,
        json: jest.fn().mockResolvedValue({
          message:
            "Exception record updated with ServiceNow number successfully",
          exceptionId: 2073,
          previousServiceNowId: "INC0987654321",
          newServiceNowId: "",
        }),
      });

      const formData = new FormData();
      formData.append("serviceNowID", "");
      formData.append("isEditMode", "false");

      await expect(updateExceptions(2073, formData)).rejects.toThrow(
        "NEXT_REDIRECT;/exceptions"
      );

      expect(fetch).toHaveBeenCalledWith(
        "https://api.example.com/api/UpdateExceptionServiceNowId",
        {
          method: "PUT",
          headers: {
            "Content-Type": "application/json",
          },
          body: JSON.stringify({
            ExceptionId: 2073,
            ServiceNowId: "",
          }),
        }
      );

      expect(mockRedirect).toHaveBeenCalledWith("/exceptions");
    });

    it("successfully updates ServiceNow ID in edit mode and redirects to raised exceptions", async () => {
      mockSafeParse.mockReturnValue({
        success: true,
        data: { serviceNowID: "INC1234567890" },
      });

      global.fetch = jest.fn().mockResolvedValue({
        ok: true,
        json: jest.fn().mockResolvedValue({
          message:
            "Exception record updated with ServiceNow number successfully",
          exceptionId: 2073,
          previousServiceNowId: "INC0000000000",
          newServiceNowId: "INC1234567890",
        }),
      });

      const formData = new FormData();
      formData.append("serviceNowID", "INC1234567890");
      formData.append("isEditMode", "true");

      await expect(updateExceptions(2073, formData)).rejects.toThrow(
        "NEXT_REDIRECT;/exceptions/raised"
      );

      expect(fetch).toHaveBeenCalledWith(
        "https://api.example.com/api/UpdateExceptionServiceNowId",
        {
          method: "PUT",
          headers: {
            "Content-Type": "application/json",
          },
          body: JSON.stringify({
            ExceptionId: 2073,
            ServiceNowId: "INC1234567890",
          }),
        }
      );

      expect(mockRedirect).toHaveBeenCalledWith("/exceptions/raised");
    });
  });

  describe("API error handling", () => {
    it("redirects with API error message when response is not ok", async () => {
      mockSafeParse.mockReturnValue({
        success: true,
        data: { serviceNowID: "INC1234567890" },
      });

      const errorMessage = "Invalid ExceptionId provided";
      global.fetch = jest.fn().mockResolvedValue({
        ok: false,
        text: jest.fn().mockResolvedValue(errorMessage),
      });

      const formData = new FormData();
      formData.append("serviceNowID", "INC1234567890");
      formData.append("isEditMode", "false");

      await expect(updateExceptions(2073, formData)).rejects.toThrow(
        `NEXT_REDIRECT;/participant-information/2073?error=${encodeURIComponent(
          errorMessage
        )}`
      );

      expect(mockRedirect).toHaveBeenCalledWith(
        `/participant-information/2073?error=${encodeURIComponent(
          errorMessage
        )}`
      );
    });

    it("redirects with API error message in edit mode", async () => {
      mockSafeParse.mockReturnValue({
        success: true,
        data: { serviceNowID: "INC1234567890" },
      });

      const errorMessage = "Exception not found";
      global.fetch = jest.fn().mockResolvedValue({
        ok: false,
        text: jest.fn().mockResolvedValue(errorMessage),
      });

      const formData = new FormData();
      formData.append("serviceNowID", "INC1234567890");
      formData.append("isEditMode", "true");

      await expect(updateExceptions(2073, formData)).rejects.toThrow(
        `NEXT_REDIRECT;/participant-information/2073?edit=true&error=${encodeURIComponent(
          errorMessage
        )}`
      );

      expect(mockRedirect).toHaveBeenCalledWith(
        `/participant-information/2073?edit=true&error=${encodeURIComponent(
          errorMessage
        )}`
      );
    });

    it("handles 500 internal server error", async () => {
      mockSafeParse.mockReturnValue({
        success: true,
        data: { serviceNowID: "INC1234567890" },
      });

      global.fetch = jest.fn().mockResolvedValue({
        ok: false,
        status: 500,
        text: jest.fn().mockResolvedValue("Internal Server Error"),
      });

      const formData = new FormData();
      formData.append("serviceNowID", "INC1234567890");
      formData.append("isEditMode", "false");

      await expect(updateExceptions(2073, formData)).rejects.toThrow(
        "NEXT_REDIRECT;/participant-information/2073?error=Internal%20Server%20Error"
      );

      expect(mockRedirect).toHaveBeenCalledWith(
        "/participant-information/2073?error=Internal%20Server%20Error"
      );
    });

    it("handles 400 bad request error", async () => {
      mockSafeParse.mockReturnValue({
        success: true,
        data: { serviceNowID: "INC1234567890" },
      });

      global.fetch = jest.fn().mockResolvedValue({
        ok: false,
        status: 400,
        text: jest.fn().mockResolvedValue("Bad Request - Invalid data"),
      });

      const formData = new FormData();
      formData.append("serviceNowID", "INC1234567890");
      formData.append("isEditMode", "false");

      await expect(updateExceptions(999999, formData)).rejects.toThrow(
        "NEXT_REDIRECT;/participant-information/999999?error=Bad%20Request%20-%20Invalid%20data"
      );

      expect(mockRedirect).toHaveBeenCalledWith(
        "/participant-information/999999?error=Bad%20Request%20-%20Invalid%20data"
      );
    });
  });

  describe("form data handling", () => {
    it("handles missing serviceNowID field", async () => {
      mockSafeParse.mockReturnValue({
        success: false,
        error: {
          issues: [{ message: "ServiceNow ID is required" }],
        },
      });

      const formData = new FormData();
      formData.append("isEditMode", "false");

      await expect(updateExceptions(2073, formData)).rejects.toThrow(
        "NEXT_REDIRECT;/participant-information/2073?error=ServiceNow%20ID%20is%20required"
      );

      expect(mockSafeParse).toHaveBeenCalledWith({ serviceNowID: null });
      expect(mockRedirect).toHaveBeenCalledWith(
        "/participant-information/2073?error=ServiceNow%20ID%20is%20required"
      );
    });

    it("handles missing isEditMode field (defaults to false)", async () => {
      mockSafeParse.mockReturnValue({
        success: false,
        error: {
          issues: [{ message: "ServiceNow ID must be alphanumeric only" }],
        },
      });

      const formData = new FormData();
      formData.append("serviceNowID", "INC@123");

      await expect(updateExceptions(2073, formData)).rejects.toThrow(
        "NEXT_REDIRECT;/participant-information/2073?error=ServiceNow%20ID%20must%20be%20alphanumeric%20only"
      );

      expect(mockRedirect).toHaveBeenCalledWith(
        "/participant-information/2073?error=ServiceNow%20ID%20must%20be%20alphanumeric%20only"
      );
    });
  });

  describe("edge cases", () => {
    it("handles different exception IDs", async () => {
      mockSafeParse.mockReturnValue({
        success: true,
        data: { serviceNowID: "INC9999999999" },
      });

      global.fetch = jest.fn().mockResolvedValue({
        ok: true,
        json: jest.fn().mockResolvedValue({
          message: "Success",
          exceptionId: 2075,
        }),
      });

      const formData = new FormData();
      formData.append("serviceNowID", "INC9999999999");
      formData.append("isEditMode", "false");

      await expect(updateExceptions(2075, formData)).rejects.toThrow(
        "NEXT_REDIRECT;/exceptions"
      );

      expect(fetch).toHaveBeenCalledWith(
        "https://api.example.com/api/UpdateExceptionServiceNowId",
        expect.objectContaining({
          body: JSON.stringify({
            ExceptionId: 2075,
            ServiceNowId: "INC9999999999",
          }),
        })
      );

      expect(mockRedirect).toHaveBeenCalledWith("/exceptions");
    });

    it("handles very long ServiceNow IDs", async () => {
      const longServiceNowId = "INC" + "1234567890".repeat(5); // 53 characters
      mockSafeParse.mockReturnValue({
        success: true,
        data: { serviceNowID: longServiceNowId },
      });

      global.fetch = jest.fn().mockResolvedValue({
        ok: true,
        json: jest.fn().mockResolvedValue({
          message: "Success",
          exceptionId: 2073,
        }),
      });

      const formData = new FormData();
      formData.append("serviceNowID", longServiceNowId);
      formData.append("isEditMode", "false");

      await expect(updateExceptions(2073, formData)).rejects.toThrow(
        "NEXT_REDIRECT;/exceptions"
      );

      expect(fetch).toHaveBeenCalledWith(
        "https://api.example.com/api/UpdateExceptionServiceNowId",
        expect.objectContaining({
          body: JSON.stringify({
            ExceptionId: 2073,
            ServiceNowId: longServiceNowId,
          }),
        })
      );
    });
  });

  describe("environment configuration", () => {
    it("constructs API URL correctly", async () => {
      process.env.EXCEPTIONS_API_URL = "https://custom-api.example.com";

      mockSafeParse.mockReturnValue({
        success: true,
        data: { serviceNowID: "INC1234567890" },
      });

      global.fetch = jest.fn().mockResolvedValue({
        ok: true,
        json: jest.fn().mockResolvedValue({
          message: "Success",
        }),
      });

      const formData = new FormData();
      formData.append("serviceNowID", "INC1234567890");
      formData.append("isEditMode", "false");

      await expect(updateExceptions(2073, formData)).rejects.toThrow(
        "NEXT_REDIRECT;/exceptions"
      );

      expect(fetch).toHaveBeenCalledWith(
        "https://custom-api.example.com/api/UpdateExceptionServiceNowId",
        expect.any(Object)
      );
    });
  });
});
