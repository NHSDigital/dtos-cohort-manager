"use server";

export async function fetchReports(exceptionCategory: number, date: string) {
  const apiUrl = `${process.env.EXCEPTIONS_API_URL}/api/GetValidationExceptions?exceptionStatus=1&sortOrder=1&exceptionCategory=${exceptionCategory}&isReport=1&reportDate=${date}`;

  const response = await fetch(apiUrl);

  if (response.status === 204) {
    return [];
  }

  if (!response.ok) {
    throw new Error(
      `Error fetching data for reports: ${response.status} ${response.statusText} from ${apiUrl}`
    );
  }

  return response.json();
}
