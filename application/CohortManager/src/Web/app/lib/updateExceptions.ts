"use server";

import { redirect } from "next/navigation";
import { updateExceptionsSchema } from "@/app/lib/formValidationSchemas";

export async function updateExceptions(
  exceptionId: number,
  formData: FormData
) {
  const serviceNowID = formData.get("serviceNowID") as string;
  const isEditMode = formData.get("isEditMode") === "true";

  const parsedData = updateExceptionsSchema(isEditMode).safeParse({
    serviceNowID,
  });

  if (!parsedData.success) {
    const firstError = parsedData.error.issues[0]?.message;

    // Redirect with error in search params, preserving edit mode if required
    const errorUrl = isEditMode
      ? `/participant-information/${exceptionId}?edit=true&error=${encodeURIComponent(
          firstError
        )}`
      : `/participant-information/${exceptionId}?error=${encodeURIComponent(
          firstError
        )}`;
    redirect(errorUrl);
  }

  const apiUrl = `${process.env.EXCEPTIONS_API_URL}/api/UpdateExceptionServiceNowId`;

  const response = await fetch(apiUrl, {
    method: "PUT",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify({
      ExceptionId: exceptionId,
      ServiceNowId: parsedData.data.serviceNowID,
    }),
  });

  if (!response.ok) {
    const errorMessage = response.text();
    // Redirect with error in search params, preserving edit mode if required
    const apiErrorUrl = isEditMode
      ? `/participant-information/${exceptionId}?edit=true&error=${encodeURIComponent(
          await errorMessage
        )}`
      : `/participant-information/${exceptionId}?error=${encodeURIComponent(
          await errorMessage
        )}`;
    redirect(apiErrorUrl);
  }

  const successUrl = isEditMode ? "/exceptions/raised" : "/exceptions";
  redirect(successUrl);
}
