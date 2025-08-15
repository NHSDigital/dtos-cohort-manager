import { z } from "zod";

// This schema validates the ServiceNow case ID for updating exceptions status.
// It ensures the ID is a non-empty string, at least 9 characters long,
// contains only alphanumeric characters, and does not include spaces.
export const updateExceptionsSchema = z.object({
  serviceNowID: z
    .string()
    .min(1, "ServiceNow case ID is required")
    .min(9, "ServiceNow case ID must be nine characters or more")
    .refine(
      (val) => !val.includes(" "),
      "ServiceNow case ID must not contain spaces"
    )
    .regex(
      /^[a-zA-Z0-9]+$/,
      "ServiceNow case ID must only contain letters and numbers"
    ),
});
