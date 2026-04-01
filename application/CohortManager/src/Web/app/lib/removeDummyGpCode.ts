"use server";

import { redirect } from "next/navigation";
import { removeDummyGpCodeSchema } from "@/app/lib/formValidationSchemas";

export type RemoveDummyGpCodeState = {
  error?: string;
  field?: string;
  values?: {
    nhsNumber: string;
    forename: string;
    surname: string;
    dobDay: string;
    dobMonth: string;
    dobYear: string;
    serviceNowTicketNumber: string;
  };
} | null;

export async function removeDummyGpCode(
  _prevState: RemoveDummyGpCodeState,
  formData: FormData
): Promise<RemoveDummyGpCodeState> {
  const nhsNumber = formData.get("nhsNumber") as string;
  const forename = formData.get("forename") as string;
  const surname = formData.get("surname") as string;
  const dobDay = formData.get("dob-day") as string;
  const dobMonth = formData.get("dob-month") as string;
  const dobYear = formData.get("dob-year") as string;
  const serviceNowTicketNumber = formData.get("serviceNowTicketNumber") as string;

  const submittedValues = {
    nhsNumber: nhsNumber ?? "",
    forename: forename ?? "",
    surname: surname ?? "",
    dobDay: dobDay ?? "",
    dobMonth: dobMonth ?? "",
    dobYear: dobYear ?? "",
    serviceNowTicketNumber: serviceNowTicketNumber ?? "",
  };

  const dateOfBirth =
    dobYear && dobMonth && dobDay
      ? `${dobYear}-${dobMonth.padStart(2, "0")}-${dobDay.padStart(2, "0")}`
      : "";

  const fieldIdMap: Record<string, string> = {
    nhsNumber: "nhsNumber",
    forename: "forename",
    surname: "surname",
    dateOfBirth: "dob-day",
    serviceNowTicketNumber: "serviceNowTicketNumber",
  };

  const parsedData = removeDummyGpCodeSchema.safeParse({
    nhsNumber,
    forename,
    surname,
    dateOfBirth,
    serviceNowTicketNumber,
  });

  if (!parsedData.success) {
    const firstIssue = parsedData.error.issues[0];
    const errorMessage = firstIssue?.message;
    const errorField = fieldIdMap[String(firstIssue?.path[0])] ?? "nhsNumber";
    return { error: errorMessage, field: errorField, values: submittedValues };
  }

  const apiUrl = `${process.env.REMOVE_DUMMY_GP_CODE_API_URL}/api/RemoveDummyGPCode`;

  let response: Response;
  try {
    response = await fetch(apiUrl, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify({
        nhs_number: parsedData.data.nhsNumber,
        forename: parsedData.data.forename,
        surname: parsedData.data.surname,
        date_of_birth: parsedData.data.dateOfBirth,
        request_id: parsedData.data.serviceNowTicketNumber,
      }),
    });
  } catch {
    return { error: "Unable to connect to the service. Please try again later.", values: submittedValues };
  }

  if (response.status === 202) {
    redirect("/remove-dummy-gp-code?success=true");
  }

  if (response.status === 400) {
    return { error: "The participant could not be found or the details provided do not match", values: submittedValues };
  }

  return { error: "An unexpected error occurred. Please try again later.", values: submittedValues };
}
