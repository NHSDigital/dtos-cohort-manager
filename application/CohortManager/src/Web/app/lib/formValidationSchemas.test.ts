import { updateExceptionsSchema, removeDummyGpCodeSchema } from "./formValidationSchemas";

describe("formValidationSchemas", () => {
  describe("updateExceptionsSchema", () => {
    describe("serviceNowID validation", () => {
      describe("valid ServiceNow case IDs", () => {
        it("should accept valid ServiceNow case ID with 2 letters and 7 digits", () => {
          const validData = { serviceNowID: "CS1234567" };
          const result = updateExceptionsSchema().safeParse(validData);

          expect(result.success).toBe(true);
          if (result.success) {
            expect(result.data.serviceNowID).toBe("CS1234567");
          }
        });

        it("should accept valid ServiceNow case ID with more than 7 digits after the two letters", () => {
          const validData = { serviceNowID: "CS123456789" };
          const result = updateExceptionsSchema().safeParse(validData);

          expect(result.success).toBe(true);
          if (result.success) {
            expect(result.data.serviceNowID).toBe("CS123456789");
          }
        });

        it("should accept case-insensitive letters prefix", () => {
          const validData = { serviceNowID: "cS1234567" };
          const result = updateExceptionsSchema().safeParse(validData);

          expect(result.success).toBe(true);
        });
      });

      describe("invalid ServiceNow case IDs - required field", () => {
        it("should reject empty string", () => {
          const invalidData = { serviceNowID: "" };
          const result = updateExceptionsSchema().safeParse(invalidData);

          expect(result.success).toBe(false);
          if (!result.success) {
            expect(result.error.issues[0].message).toBe(
              "ServiceNow case ID is required"
            );
          }
        });

        it("should reject missing serviceNowID field", () => {
          const invalidData = {};
          const result = updateExceptionsSchema().safeParse(invalidData);

          expect(result.success).toBe(false);
          if (!result.success) {
            expect(result.error.issues[0].code).toBe("invalid_type");
          }
        });
      });

      describe("invalid ServiceNow case IDs - format and length", () => {
        it("should reject case ID with fewer than 7 digits after two letters (length error first)", () => {
          const invalidData = { serviceNowID: "CS12345" };
          const result = updateExceptionsSchema().safeParse(invalidData);

          expect(result.success).toBe(false);
          if (!result.success) {
            expect(result.error.issues[0].message).toBe(
              "ServiceNow case ID must be nine characters or more"
            );
          }
        });

        it("should reject single character", () => {
          const invalidData = { serviceNowID: "C" };
          const result = updateExceptionsSchema().safeParse(invalidData);

          expect(result.success).toBe(false);
          if (!result.success) {
            expect(result.error.issues[0].message).toBe(
              "ServiceNow case ID must be nine characters or more"
            );
          }
        });

        it("should reject 8 characters", () => {
          const invalidData = { serviceNowID: "CS123456" };
          const result = updateExceptionsSchema().safeParse(invalidData);

          expect(result.success).toBe(false);
          if (!result.success) {
            expect(result.error.issues[0].message).toBe(
              "ServiceNow case ID must be nine characters or more"
            );
          }
        });
      });

      describe("invalid ServiceNow case IDs - special characters", () => {
        it("should reject case ID with special characters", () => {
          const invalidData = { serviceNowID: "CS1234567!" };
          const result = updateExceptionsSchema().safeParse(invalidData);

          expect(result.success).toBe(false);
          if (!result.success) {
            expect(result.error.issues[0].message).toBe(
              "ServiceNow case ID must only contain letters and numbers"
            );
          }
        });

        it("should reject case ID with hyphen", () => {
          const invalidData = { serviceNowID: "CS-1234567" };
          const result = updateExceptionsSchema().safeParse(invalidData);

          expect(result.success).toBe(false);
          if (!result.success) {
            expect(result.error.issues[0].message).toBe(
              "ServiceNow case ID must only contain letters and numbers"
            );
          }
        });

        it("should reject case ID with underscore", () => {
          const invalidData = { serviceNowID: "CS_1234567" };
          const result = updateExceptionsSchema().safeParse(invalidData);

          expect(result.success).toBe(false);
          if (!result.success) {
            expect(result.error.issues[0].message).toBe(
              "ServiceNow case ID must only contain letters and numbers"
            );
          }
        });

        it("should reject case ID with period", () => {
          const invalidData = { serviceNowID: "CS.1234567" };
          const result = updateExceptionsSchema().safeParse(invalidData);

          expect(result.success).toBe(false);
          if (!result.success) {
            expect(result.error.issues[0].message).toBe(
              "ServiceNow case ID must only contain letters and numbers"
            );
          }
        });

        it("should reject case ID with @ symbol", () => {
          const invalidData = { serviceNowID: "CS@1234567" };
          const result = updateExceptionsSchema().safeParse(invalidData);

          expect(result.success).toBe(false);
          if (!result.success) {
            expect(result.error.issues[0].message).toBe(
              "ServiceNow case ID must only contain letters and numbers"
            );
          }
        });
      });

      describe("invalid ServiceNow case IDs - spaces", () => {
        it("should reject case ID with spaces", () => {
          const invalidData = { serviceNowID: "CS 1234567" };
          const result = updateExceptionsSchema().safeParse(invalidData);

          expect(result.success).toBe(false);
          if (!result.success) {
            expect(result.error.issues[0].message).toBe(
              "ServiceNow case ID must not contain spaces"
            );
          }
        });

        it("should reject case ID with leading space", () => {
          const invalidData = { serviceNowID: " CS1234567" };
          const result = updateExceptionsSchema().safeParse(invalidData);

          expect(result.success).toBe(false);
          if (!result.success) {
            expect(result.error.issues[0].message).toBe(
              "ServiceNow case ID must not contain spaces"
            );
          }
        });

        it("should reject case ID with trailing space", () => {
          const invalidData = { serviceNowID: "CS1234567 " };
          const result = updateExceptionsSchema().safeParse(invalidData);

          expect(result.success).toBe(false);
          if (!result.success) {
            expect(result.error.issues[0].message).toBe(
              "ServiceNow case ID must not contain spaces"
            );
          }
        });

        it("should reject case ID with multiple spaces", () => {
          const invalidData = { serviceNowID: "CS  1234567" };
          const result = updateExceptionsSchema().safeParse(invalidData);

          expect(result.success).toBe(false);
          if (!result.success) {
            expect(result.error.issues[0].message).toBe(
              "ServiceNow case ID must not contain spaces"
            );
          }
        });
      });

      describe("validation error priority", () => {
        it("should prioritize required error over length error", () => {
          const invalidData = { serviceNowID: "" };
          const result = updateExceptionsSchema().safeParse(invalidData);

          expect(result.success).toBe(false);
          if (!result.success) {
            expect(result.error.issues[0].message).toBe(
              "ServiceNow case ID is required"
            );
          }
        });

        it("should show space error when spaces present (before other validations)", () => {
          const invalidData = { serviceNowID: "CS123 4567" };
          const result = updateExceptionsSchema().safeParse(invalidData);

          expect(result.success).toBe(false);
          if (!result.success) {
            expect(result.error.issues[0].message).toBe(
              "ServiceNow case ID must not contain spaces"
            );
          }
        });

        it("should show space refine error before regex error", () => {
          const invalidData = { serviceNowID: "CS1234567 !" };
          const result = updateExceptionsSchema().safeParse(invalidData);

          expect(result.success).toBe(false);
          if (!result.success) {
            expect(result.error.issues[0].message).toBe(
              "ServiceNow case ID must not contain spaces"
            );
          }
        });
      });

      describe("edit mode behavior", () => {
        it("should accept empty ServiceNow ID in edit mode", () => {
          const validData = { serviceNowID: "" };
          const result = updateExceptionsSchema(true).safeParse(validData);

          expect(result.success).toBe(true);
          if (result.success) {
            expect(result.data.serviceNowID).toBe("");
          }
        });

        it("should reject empty ServiceNow ID in non-edit mode", () => {
          const invalidData = { serviceNowID: "" };
          const result = updateExceptionsSchema(false).safeParse(invalidData);

          expect(result.success).toBe(false);
          if (!result.success) {
            expect(result.error.issues).toEqual(
              expect.arrayContaining([
                expect.objectContaining({
                  message: "ServiceNow case ID is required",
                }),
              ])
            );
          }
        });

        it("should still validate format when not empty in edit mode", () => {
          const invalidData = { serviceNowID: "short" };
          const result = updateExceptionsSchema(true).safeParse(invalidData);

          expect(result.success).toBe(false);
          if (!result.success) {
            expect(result.error.issues).toEqual(
              expect.arrayContaining([
                expect.objectContaining({
                  message: "ServiceNow case ID must be nine characters or more",
                }),
              ])
            );
          }
        });

        it("should fail the final pattern when letters count is not two", () => {
          // Pass earlier checks: length>=9, no spaces, alphanumeric only
          const invalidData = { serviceNowID: "ABCD12345" }; // 4 letters, 5 digits
          const result = updateExceptionsSchema(true).safeParse(invalidData);

          expect(result.success).toBe(false);
          if (!result.success) {
            expect(result.error.issues).toEqual(
              expect.arrayContaining([
                expect.objectContaining({
                  message:
                    "ServiceNow case ID must start with two letters followed by at least seven digits (e.g. CS0619153)",
                }),
              ])
            );
          }
        });

        it("should fail the final pattern when digits come first", () => {
          const invalidData = { serviceNowID: "123456789" }; // 9 digits, no letters
          const result = updateExceptionsSchema(true).safeParse(invalidData);

          expect(result.success).toBe(false);
          if (!result.success) {
            expect(result.error.issues[0].message).toBe(
              "ServiceNow case ID must start with two letters followed by at least seven digits (e.g. CS0619153)"
            );
          }
        });

        it("should accept valid ServiceNow ID in edit mode", () => {
          const validData = { serviceNowID: "CS1234567" };
          const result = updateExceptionsSchema(true).safeParse(validData);

          expect(result.success).toBe(true);
          if (result.success) {
            expect(result.data.serviceNowID).toBe("CS1234567");
          }
        });
      });
    });
  });

  describe("removeDummyGpCodeSchema", () => {
    describe("valid submissions", () => {
      it("should accept valid form data", () => {
        const validData = {
          nhsNumber: "9434765919",
          forename: "Jane",
          surname: "Smith",
          dateOfBirth: "1990-01-15",
          serviceNowTicketNumber: "CS1234567",
        };
        const result = removeDummyGpCodeSchema.safeParse(validData);
        expect(result.success).toBe(true);
      });
    });

    describe("NHS Number validation", () => {
      it("should reject empty NHS Number", () => {
        const data = {
          nhsNumber: "",
          forename: "Jane",
          surname: "Smith",
          dateOfBirth: "1990-01-15",
          serviceNowTicketNumber: "CS1234567",
        };
        const result = removeDummyGpCodeSchema.safeParse(data);
        expect(result.success).toBe(false);
        if (!result.success) {
          expect(result.error.issues[0].message).toBe("NHS Number is required");
        }
      });

      it("should reject NHS Number with fewer than 10 digits", () => {
        const data = {
          nhsNumber: "123456789",
          forename: "Jane",
          surname: "Smith",
          dateOfBirth: "1990-01-15",
          serviceNowTicketNumber: "CS1234567",
        };
        const result = removeDummyGpCodeSchema.safeParse(data);
        expect(result.success).toBe(false);
        if (!result.success) {
          expect(result.error.issues[0].message).toBe("NHS Number must be 10 digits");
        }
      });

      it("should reject NHS Number with more than 10 digits", () => {
        const data = {
          nhsNumber: "12345678901",
          forename: "Jane",
          surname: "Smith",
          dateOfBirth: "1990-01-15",
          serviceNowTicketNumber: "CS1234567",
        };
        const result = removeDummyGpCodeSchema.safeParse(data);
        expect(result.success).toBe(false);
        if (!result.success) {
          expect(result.error.issues[0].message).toBe("NHS Number must be 10 digits");
        }
      });

      it("should reject NHS Number with letters", () => {
        const data = {
          nhsNumber: "12345ABCDE",
          forename: "Jane",
          surname: "Smith",
          dateOfBirth: "1990-01-15",
          serviceNowTicketNumber: "CS1234567",
        };
        const result = removeDummyGpCodeSchema.safeParse(data);
        expect(result.success).toBe(false);
        if (!result.success) {
          expect(result.error.issues[0].message).toBe("NHS Number must be 10 digits");
        }
      });

      it("should reject NHS Number with invalid checksum", () => {
        const data = {
          nhsNumber: "1234567890",
          forename: "Jane",
          surname: "Smith",
          dateOfBirth: "1990-01-15",
          serviceNowTicketNumber: "CS1234567",
        };
        const result = removeDummyGpCodeSchema.safeParse(data);
        expect(result.success).toBe(false);
        if (!result.success) {
          expect(result.error.issues[0].message).toBe("Invalid NHS Number");
        }
      });

      it("should reject all-zeros NHS Number", () => {
        const data = {
          nhsNumber: "0000000000",
          forename: "Jane",
          surname: "Smith",
          dateOfBirth: "1990-01-15",
          serviceNowTicketNumber: "CS1234567",
        };
        const result = removeDummyGpCodeSchema.safeParse(data);
        expect(result.success).toBe(false);
        if (!result.success) {
          expect(result.error.issues[0].message).toBe("Invalid NHS Number");
        }
      });
    });

    describe("Forename validation", () => {
      it("should reject empty forename", () => {
        const data = {
          nhsNumber: "9434765919",
          forename: "",
          surname: "Smith",
          dateOfBirth: "1990-01-15",
          serviceNowTicketNumber: "CS1234567",
        };
        const result = removeDummyGpCodeSchema.safeParse(data);
        expect(result.success).toBe(false);
        if (!result.success) {
          expect(result.error.issues[0].message).toBe("Forename is required");
        }
      });
    });

    describe("Surname validation", () => {
      it("should reject empty surname", () => {
        const data = {
          nhsNumber: "9434765919",
          forename: "Jane",
          surname: "",
          dateOfBirth: "1990-01-15",
          serviceNowTicketNumber: "CS1234567",
        };
        const result = removeDummyGpCodeSchema.safeParse(data);
        expect(result.success).toBe(false);
        if (!result.success) {
          expect(result.error.issues[0].message).toBe("Surname is required");
        }
      });
    });

    describe("Date of Birth validation", () => {
      it("should reject empty date of birth", () => {
        const data = {
          nhsNumber: "9434765919",
          forename: "Jane",
          surname: "Smith",
          dateOfBirth: "",
          serviceNowTicketNumber: "CS1234567",
        };
        const result = removeDummyGpCodeSchema.safeParse(data);
        expect(result.success).toBe(false);
        if (!result.success) {
          expect(result.error.issues[0].message).toBe("Date of Birth is required");
        }
      });

      it("should reject invalid date string", () => {
        const data = {
          nhsNumber: "9434765919",
          forename: "Jane",
          surname: "Smith",
          dateOfBirth: "not-a-date",
          serviceNowTicketNumber: "CS1234567",
        };
        const result = removeDummyGpCodeSchema.safeParse(data);
        expect(result.success).toBe(false);
        if (!result.success) {
          expect(result.error.issues[0].message).toBe("Date of Birth must be a valid date");
        }
      });
    });

    describe("Service Now Ticket Number validation", () => {
      it("should reject empty ticket number", () => {
        const data = {
          nhsNumber: "9434765919",
          forename: "Jane",
          surname: "Smith",
          dateOfBirth: "1990-01-15",
          serviceNowTicketNumber: "",
        };
        const result = removeDummyGpCodeSchema.safeParse(data);
        expect(result.success).toBe(false);
        if (!result.success) {
          expect(result.error.issues[0].message).toBe("Service Now Ticket Number is required");
        }
      });

      it("should reject ticket number shorter than 9 characters", () => {
        const data = {
          nhsNumber: "9434765919",
          forename: "Jane",
          surname: "Smith",
          dateOfBirth: "1990-01-15",
          serviceNowTicketNumber: "CS12345",
        };
        const result = removeDummyGpCodeSchema.safeParse(data);
        expect(result.success).toBe(false);
        if (!result.success) {
          expect(result.error.issues[0].message).toBe(
            "Service Now Ticket Number must be nine characters or more"
          );
        }
      });

      it("should reject ticket number with spaces", () => {
        const data = {
          nhsNumber: "9434765919",
          forename: "Jane",
          surname: "Smith",
          dateOfBirth: "1990-01-15",
          serviceNowTicketNumber: "CS 1234567",
        };
        const result = removeDummyGpCodeSchema.safeParse(data);
        expect(result.success).toBe(false);
        if (!result.success) {
          expect(result.error.issues[0].message).toBe(
            "Service Now Ticket Number must not contain spaces"
          );
        }
      });

      it("should reject ticket number with invalid format", () => {
        const data = {
          nhsNumber: "9434765919",
          forename: "Jane",
          surname: "Smith",
          dateOfBirth: "1990-01-15",
          serviceNowTicketNumber: "123456789",
        };
        const result = removeDummyGpCodeSchema.safeParse(data);
        expect(result.success).toBe(false);
        if (!result.success) {
          expect(result.error.issues[0].message).toBe(
            "Service Now Ticket Number must start with two letters followed by at least seven digits (e.g. CS0619153)"
          );
        }
      });
    });
  });
});
