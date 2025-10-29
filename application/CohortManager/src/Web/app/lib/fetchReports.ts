"use server";

export async function fetchReports(
  exceptionCategory: number,
  date: string,
  pageSize: number,
  page?: number
) {
  const query = new URLSearchParams({
    exceptionStatus: "1",
    sortOrder: "1",
    exceptionCategory: exceptionCategory.toString(),
    isReport: "1",
    reportDate: date,
    pageSize: pageSize.toString(),
  });

  if (page) {
    query.append("page", page.toString());
  }

  const apiUrl = `${process.env.EXCEPTIONS_API_URL}/api/GetValidationExceptions?${query.toString()}`;

  const response = await fetch(apiUrl);
  if (!response.ok) {
    throw new Error(
      `Error fetching data for reports: ${response.status} ${response.statusText} from ${apiUrl}`
    );
  }

  const data = await response.json();
  const linkHeader = response.headers.get("Link");

  return {
    data,
    linkHeader,
    headers: response.headers,
  };
}
