import { z } from "zod";

// This schema validates the ServiceNow case ID for updating exceptions status.
// In edit mode, empty input is allowed to clear the ServiceNow ID (convert raised to non-raised).
// In non-edit mode, the ID is required and must start with two letters followed by at least seven digits (e.g. CS0619153).
export const updateExceptionsSchema = (isEditMode: boolean = false) =>
  z.object({
    serviceNowID: z
      .string()
      .refine((val) => {
        // In edit mode, allow empty string to clear the ServiceNow ID
        if (isEditMode && val === "") {
          return true;
        }
        // Otherwise, require at least 1 character
        return val.length >= 1;
      }, "ServiceNow case ID is required")
      .refine((val) => {
        if (isEditMode && val === "") {
          return true;
        }
        // Otherwise, require at least 9 characters
        return val.length >= 9;
      }, "ServiceNow case ID must be nine characters or more")
      .refine((val) => {
        if (isEditMode && val === "") {
          return true;
        }
        // Otherwise, check for spaces
        return !val.includes(" ");
      }, "ServiceNow case ID must not contain spaces")
      .refine((val) => {
        if (isEditMode && val === "") {
          return true;
        }
        // Otherwise, check alphanumeric pattern
        return /^[a-zA-Z0-9]+$/.test(val);
      }, "ServiceNow case ID must only contain letters and numbers")
      .refine((val) => {
        if (isEditMode && val === "") {
          return true;
        }
        // Finally, enforce two letters followed by at least seven digits (e.g. CS0619153)
        return /^[A-Za-z]{2}\d{7,}$/.test(val);
      }, "ServiceNow case ID must start with two letters followed by at least seven digits (e.g. CS0619153)"),
  });
export const removeDummyGpCodeSchema = z.object({
  nhsNumber: z
    .string()
    .min(1, "NHS Number is required")
    .regex(/^\d{10}$/, "NHS Number must be 10 digits")
    .refine((val) => {
      if (val === "0000000000") return false;
      let sum = 0;
      for (let i = 0; i < 9; i++) {
        sum += Number.parseInt(val[i], 10) * (10 - i);
      }
      const checkDigit = 11 - (sum % 11);
      if (checkDigit === 10) return false;
      const expected = checkDigit === 11 ? 0 : checkDigit;
      return Number.parseInt(val[9], 10) === expected;
    }, "Invalid NHS Number"),
  forename: z
    .string()
    .min(1, "Forename is required"),
  surname: z
    .string()
    .min(1, "Surname is required"),
  dateOfBirth: z
    .string()
    .min(1, "Date of Birth is required")
    .refine((val) => {
      const date = new Date(val);
      return !Number.isNaN(date.getTime());
    }, "Date of Birth must be a valid date"),
  serviceNowTicketNumber: z
    .string()
    .min(1, "Service Now Ticket Number is required")
    .min(9, "Service Now Ticket Number must be nine characters or more")
    .refine((val) => !val.includes(" "), "Service Now Ticket Number must not contain spaces")
    .refine((val) => /^[a-zA-Z0-9]+$/.test(val), "Service Now Ticket Number must only contain letters and numbers")
    .refine((val) => /^[A-Za-z]{2}\d{7,}$/.test(val), "Service Now Ticket Number must start with two letters followed by at least seven digits (e.g. CS0619153)"
    ),
});
