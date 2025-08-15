import { updateExceptionsSchema } from "./formValidationSchemas";

describe("formValidationSchemas", () => {
  describe("updateExceptionsSchema", () => {
    describe("serviceNowID validation", () => {
      describe("valid ServiceNow case IDs", () => {
        it("should accept valid ServiceNow case ID with 9 characters", () => {
          const validData = { serviceNowID: "CS1234567" };
          const result = updateExceptionsSchema.safeParse(validData);

          expect(result.success).toBe(true);
          if (result.success) {
            expect(result.data.serviceNowID).toBe("CS1234567");
          }
        });

        it("should accept valid ServiceNow case ID with more than 9 characters", () => {
          const validData = { serviceNowID: "CS123456789" };
          const result = updateExceptionsSchema.safeParse(validData);

          expect(result.success).toBe(true);
          if (result.success) {
            expect(result.data.serviceNowID).toBe("CS123456789");
          }
        });

        it("should accept all uppercase letters", () => {
          const validData = { serviceNowID: "ABCD12345" };
          const result = updateExceptionsSchema.safeParse(validData);

          expect(result.success).toBe(true);
        });

        it("should accept all lowercase letters", () => {
          const validData = { serviceNowID: "abcd12345" };
          const result = updateExceptionsSchema.safeParse(validData);

          expect(result.success).toBe(true);
        });

        it("should accept mixed case letters", () => {
          const validData = { serviceNowID: "AbCd12345" };
          const result = updateExceptionsSchema.safeParse(validData);

          expect(result.success).toBe(true);
        });

        it("should accept all numbers", () => {
          const validData = { serviceNowID: "123456789" };
          const result = updateExceptionsSchema.safeParse(validData);

          expect(result.success).toBe(true);
        });

        it("should accept all letters", () => {
          const validData = { serviceNowID: "ABCDEFGHI" };
          const result = updateExceptionsSchema.safeParse(validData);

          expect(result.success).toBe(true);
        });
      });

      describe("invalid ServiceNow case IDs - required field", () => {
        it("should reject empty string", () => {
          const invalidData = { serviceNowID: "" };
          const result = updateExceptionsSchema.safeParse(invalidData);

          expect(result.success).toBe(false);
          if (!result.success) {
            expect(result.error.issues[0].message).toBe(
              "ServiceNow case ID is required"
            );
          }
        });

        it("should reject missing serviceNowID field", () => {
          const invalidData = {};
          const result = updateExceptionsSchema.safeParse(invalidData);

          expect(result.success).toBe(false);
          if (!result.success) {
            expect(result.error.issues[0].code).toBe("invalid_type");
          }
        });
      });

      describe("invalid ServiceNow case IDs - minimum length", () => {
        it("should reject case ID with less than 9 characters", () => {
          const invalidData = { serviceNowID: "CS12345" };
          const result = updateExceptionsSchema.safeParse(invalidData);

          expect(result.success).toBe(false);
          if (!result.success) {
            expect(result.error.issues[0].message).toBe(
              "ServiceNow case ID must be nine characters or more"
            );
          }
        });

        it("should reject single character", () => {
          const invalidData = { serviceNowID: "C" };
          const result = updateExceptionsSchema.safeParse(invalidData);

          expect(result.success).toBe(false);
          if (!result.success) {
            expect(result.error.issues[0].message).toBe(
              "ServiceNow case ID must be nine characters or more"
            );
          }
        });

        it("should reject 8 characters", () => {
          const invalidData = { serviceNowID: "CS123456" };
          const result = updateExceptionsSchema.safeParse(invalidData);

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
          const result = updateExceptionsSchema.safeParse(invalidData);

          expect(result.success).toBe(false);
          if (!result.success) {
            expect(result.error.issues[0].message).toBe(
              "ServiceNow case ID must only contain letters and numbers"
            );
          }
        });

        it("should reject case ID with hyphen", () => {
          const invalidData = { serviceNowID: "CS-1234567" };
          const result = updateExceptionsSchema.safeParse(invalidData);

          expect(result.success).toBe(false);
          if (!result.success) {
            expect(result.error.issues[0].message).toBe(
              "ServiceNow case ID must only contain letters and numbers"
            );
          }
        });

        it("should reject case ID with underscore", () => {
          const invalidData = { serviceNowID: "CS_1234567" };
          const result = updateExceptionsSchema.safeParse(invalidData);

          expect(result.success).toBe(false);
          if (!result.success) {
            expect(result.error.issues[0].message).toBe(
              "ServiceNow case ID must only contain letters and numbers"
            );
          }
        });

        it("should reject case ID with period", () => {
          const invalidData = { serviceNowID: "CS.1234567" };
          const result = updateExceptionsSchema.safeParse(invalidData);

          expect(result.success).toBe(false);
          if (!result.success) {
            expect(result.error.issues[0].message).toBe(
              "ServiceNow case ID must only contain letters and numbers"
            );
          }
        });

        it("should reject case ID with @ symbol", () => {
          const invalidData = { serviceNowID: "CS@1234567" };
          const result = updateExceptionsSchema.safeParse(invalidData);

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
          const result = updateExceptionsSchema.safeParse(invalidData);

          expect(result.success).toBe(false);
          if (!result.success) {
            expect(result.error.issues[0].message).toBe(
              "ServiceNow case ID must not contain spaces"
            );
          }
        });

        it("should reject case ID with leading space", () => {
          const invalidData = { serviceNowID: " CS1234567" };
          const result = updateExceptionsSchema.safeParse(invalidData);

          expect(result.success).toBe(false);
          if (!result.success) {
            expect(result.error.issues[0].message).toBe(
              "ServiceNow case ID must not contain spaces"
            );
          }
        });

        it("should reject case ID with trailing space", () => {
          const invalidData = { serviceNowID: "CS1234567 " };
          const result = updateExceptionsSchema.safeParse(invalidData);

          expect(result.success).toBe(false);
          if (!result.success) {
            expect(result.error.issues[0].message).toBe(
              "ServiceNow case ID must not contain spaces"
            );
          }
        });

        it("should reject case ID with multiple spaces", () => {
          const invalidData = { serviceNowID: "CS  1234567" };
          const result = updateExceptionsSchema.safeParse(invalidData);

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
          const result = updateExceptionsSchema.safeParse(invalidData);

          expect(result.success).toBe(false);
          if (!result.success) {
            expect(result.error.issues[0].message).toBe(
              "ServiceNow case ID is required"
            );
          }
        });

        it("should show length error before regex error for short invalid strings", () => {
          const invalidData = { serviceNowID: "CS!" };
          const result = updateExceptionsSchema.safeParse(invalidData);

          expect(result.success).toBe(false);
          if (!result.success) {
            expect(result.error.issues[0].message).toBe(
              "ServiceNow case ID must be nine characters or more"
            );
          }
        });

        it("should show refine error before regex error", () => {
          const invalidData = { serviceNowID: "CS1234567 !" };
          const result = updateExceptionsSchema.safeParse(invalidData);

          expect(result.success).toBe(false);
          if (!result.success) {
            expect(result.error.issues[0].message).toBe(
              "ServiceNow case ID must not contain spaces"
            );
          }
        });
      });
    });
  });
});
