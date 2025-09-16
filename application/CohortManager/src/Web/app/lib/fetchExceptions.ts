"use server";

type FetchExceptionsParams = {
  exceptionId?: number;
  page?: number;
  lastId?: number;
  exceptionStatus?: 0 | 1 | 2;
  sortOrder?: 0 | 1;
  exceptionCategory?: string;
  reportDate?: string;
  isReport?: boolean;
};

export async function fetchExceptions(params: FetchExceptionsParams = {}) {
  const query = new URLSearchParams();

  if (params.exceptionId)
    query.append("exceptionId", params.exceptionId.toString());
  if (params.page) query.append("page", params.page.toString());
  if (params.lastId) query.append("lastId", params.lastId.toString());
  if (params.exceptionStatus !== undefined)
    query.append("exceptionStatus", params.exceptionStatus.toString());
  if (params.sortOrder !== undefined)
    query.append("sortOrder", params.sortOrder.toString());
  if (params.exceptionCategory)
    query.append("exceptionCategory", params.exceptionCategory);
  if (params.reportDate) query.append("reportDate", params.reportDate);
  if (params.isReport !== undefined)
    query.append("isReport", params.isReport.toString());

  const apiUrl = `${
    process.env.EXCEPTIONS_API_URL
  }/api/GetValidationExceptions?${query.toString()}`;

  const response = await fetch(apiUrl);
  if (!response.ok) {
    throw new Error(`Error fetching data: ${response.statusText}`);
  }

  const data = await response.json();
  const linkHeader = response.headers.get("Link");

  return {
    data,
    linkHeader,
    headers: response.headers,
  };
}
