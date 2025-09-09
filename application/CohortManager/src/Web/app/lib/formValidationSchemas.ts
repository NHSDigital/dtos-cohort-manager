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
