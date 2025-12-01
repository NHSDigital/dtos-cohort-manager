"use server";

type FetchExceptionsParams = {
  exceptionId?: number;
  page?: number;
  lastId?: number;
  exceptionStatus?: 0 | 1 | 2;
  sortOrder?: 0 | 1;
  exceptionCategory?: string | number;
  reportDate?: string;
  isReport?: boolean;
  pageSize?: number;
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
  if (params.exceptionCategory !== undefined)
    query.append("exceptionCategory", params.exceptionCategory.toString());
  if (params.reportDate) query.append("reportDate", params.reportDate);
  if (params.isReport !== undefined)
    query.append("isReport", params.isReport.toString());
  query.append("pageSize", (params.pageSize ?? 10).toString());

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

type FetchExceptionsByNhsNumberParams = {
  nhsNumber: string;
  page?: number;
  pageSize?: number;
};

export async function fetchExceptionsByNhsNumber(
  params: FetchExceptionsByNhsNumberParams
) {
  const query = new URLSearchParams();

  query.append("nhsNumber", params.nhsNumber);
  query.append("page", (params.page ?? 1).toString());
  query.append("pageSize", (params.pageSize ?? 10).toString());

  const apiUrl = `${
    process.env.EXCEPTIONS_API_URL
  }/api/GetValidationExceptionsByNhsNumber?${query.toString()}`;

  const response = await fetch(apiUrl);

  // If 404, return empty result structure instead of throwing
  if (response.status === 404) {
    return {
      data: {
        NhsNumber: params.nhsNumber,
        Exceptions: {
          Items: [],
          TotalCount: 0,
          Page: params.page ?? 1,
          PageSize: params.pageSize ?? 10,
          TotalPages: 0,
          HasNextPage: false,
          HasPreviousPage: false,
        },
        Reports: [],
      },
      linkHeader: null,
      headers: response.headers,
    };
  }

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
