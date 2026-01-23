"use server";

type FetchExceptionsParams = {
  exceptionId?: number;
  page?: number;
  lastId?: number;
  exceptionStatus?: 0 | 1 | 2;
  sortOrder?: 0 | 1;
  sortBy?: 0 | 1 | 2;
  exceptionCategory?: string | number;
  reportDate?: string;
  isReport?: boolean;
  pageSize?: number;
};

export async function fetchExceptions(params: FetchExceptionsParams = {}) {
  const query = buildQueryString({
    ...params,
    pageSize: params.pageSize ?? 10
  });

  const apiUrl = `${process.env.EXCEPTIONS_API_URL}/api/GetValidationExceptions?${query}`;
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

function buildQueryString(params: Record<string, number | string | boolean | undefined>): string {
  return new URLSearchParams(
    Object.entries(params)
      .filter(([, value]) => value !== undefined && value !== null)
      .map(([key, value]) => [key, String(value)])
  ).toString();
}

type FetchExceptionsByTypeParams = {
  searchType: "NhsNumber" | "ExceptionId";
  searchValue: string;
  page?: number;
  pageSize?: number;
};

export async function fetchExceptionsByType(
  params: FetchExceptionsByTypeParams
) {
  const query = new URLSearchParams();

  query.append("searchType", params.searchType);
  query.append("searchValue", params.searchValue);
  query.append("page", (params.page ?? 1).toString());
  query.append("pageSize", (params.pageSize ?? 10).toString());

  const apiUrl = `${
    process.env.EXCEPTIONS_API_URL
  }/api/GetValidationExceptionsByType?${query.toString()}`;

  const response = await fetch(apiUrl);

  if (response.status === 204 || response.status === 404) {
    return {
      data: {
        SearchType: params.searchType === "NhsNumber" ? 0 : 1,
        SearchValue: params.searchValue,
        PaginatedExceptions: {
          Items: [],
          TotalItems: 0,
          CurrentPage: params.page ?? 1,
          TotalPages: 0,
          HasNextPage: false,
          HasPreviousPage: false,
          IsFirstPage: true,
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
