import { NextResponse } from "next/server";
import mockDataStore from "@/app/data/mockDataStore";
import { SortBy } from "@/app/lib/enums/sortBy";
import { SortOrder } from "@/app/lib/enums/sortOrder";
import { ExceptionStatus } from "@/app/lib/enums/exceptionStatus";

function sortExceptions<
  T extends {
    DateCreated: string;
    ServiceNowCreatedDate?: string;
    ExceptionId?: number;
    NhsNumber?: string
  }
>(items: T[], sortByParam: string | null, sortOrderParam: string | null = null): T[] {
  if (!sortByParam || !sortOrderParam) {
    return items;
  }

  const field = Number.parseInt(sortByParam) as SortBy;
  const order = Number.parseInt(sortOrderParam) as SortOrder;

  const sortTypes = {
    [SortBy.DateCreated]: (a: T, b: T) => {
      const diff = new Date(a.DateCreated).getTime() - new Date(b.DateCreated).getTime();
      return order === SortOrder.Descending ? -diff : diff;
    },
    [SortBy.NhsNumber]: (a: T, b: T) => {
      const cmp = (a.NhsNumber || "").localeCompare(b.NhsNumber || "");
      return order === SortOrder.Descending ? -cmp : cmp;
    },
    [SortBy.ExceptionId]: (a: T, b: T) => {
      const diff = (a.ExceptionId || 0) - (b.ExceptionId || 0);
      return order === SortOrder.Descending ? -diff : diff;
    }
  };

  const compare = sortTypes[field];
  return compare ? items.sort(compare) : items;
}

function addExceptionDetails<T extends { ExceptionId: number }>(items: T[]) {
  const exceptions = mockDataStore.getExceptions();
  return items.map((item) => ({
    ...item,
    ExceptionDetails: exceptions[item.ExceptionId]?.ExceptionDetails ?? null,
  }));
}

const PAGE_SIZE = 10;

type PaginatedResponse<T> = {
  Items: T[];
  IsFirstPage: boolean;
  HasNextPage: boolean;
  HasPreviousPage: boolean;
  LastResultId: number | null;
  TotalItems: number;
  TotalPages: number;
  CurrentPage: number;
  PageSize: number;
};

function paginate<T extends { ExceptionId: number }>(
  items: T[],
  page: number,
  pageSize = PAGE_SIZE
): PaginatedResponse<T> {
  const totalItems = items.length;
  const totalPages = Math.ceil(totalItems / pageSize);
  const safeTotalPages = Math.max(0, totalPages);
  const clampedPage = Math.max(1, Math.min(page, Math.max(1, safeTotalPages)));
  const start = (clampedPage - 1) * pageSize;
  const pageItems = items.slice(start, start + pageSize);

  return {
    Items: pageItems,
    IsFirstPage: clampedPage === 1,
    HasNextPage: clampedPage < safeTotalPages,
    HasPreviousPage: clampedPage > 1,
    LastResultId: pageItems.at(-1)?.ExceptionId ?? null,
    TotalItems: totalItems,
    TotalPages: safeTotalPages,
    CurrentPage: clampedPage,
    PageSize: pageSize,
  };
}

function buildLinkHeader(
  url: URL,
  pagination: PaginatedResponse<{ ExceptionId: number }>
): string | undefined {
  const baseUrl = `${url.origin}${url.pathname}`;
  const params = new URLSearchParams(url.search);
  params.delete("page");
  const baseQuery = params.toString();

  const buildBaseHref = (b: string, q: string) => (q ? b + "?" + q : b);

  const buildPageHref = (page: number) => {
    if (page <= 1) return buildBaseHref(baseUrl, baseQuery);
    const sep = baseQuery ? "&" : "?";
    return `${buildBaseHref(baseUrl, baseQuery)}${sep}page=${page}`;
  };

  const links: string[] = [
    `<${buildBaseHref(baseUrl, baseQuery)}>; rel="first"`,
  ];

  const candidates = [
    pagination.HasPreviousPage
      ? {
          href: buildPageHref(pagination.CurrentPage - 1),
          rel: "prev" as const,
        }
      : null,
    pagination.HasNextPage
      ? {
          href: buildPageHref(pagination.CurrentPage + 1),
          rel: "next" as const,
        }
      : null,
    pagination.TotalPages > 1
      ? { href: buildPageHref(pagination.TotalPages), rel: "last" as const }
      : null,
  ].filter(Boolean) as Array<{ href: string; rel: "prev" | "next" | "last" }>;

  for (const { href, rel } of candidates) {
    links.push(`<${href}>; rel="${rel}"`);
  }

  return links.length ? links.join(", ") : undefined;
}

function addPaginationHeaders<T extends { ExceptionId: number }>(
  response: NextResponse,
  url: URL,
  pagination: PaginatedResponse<T>
): void {
  response.headers.set("X-Total-Count", String(pagination.TotalItems));
  response.headers.set("X-Has-Next-Page", String(pagination.HasNextPage));
  response.headers.set(
    "X-Has-Previous-Page",
    String(pagination.HasPreviousPage)
  );
  response.headers.set("X-Is-First-Page", String(pagination.IsFirstPage));
  response.headers.set("X-Current-Page", String(pagination.CurrentPage));
  response.headers.set("X-Total-Pages", String(pagination.TotalPages));
  const link = buildLinkHeader(
    url,
    pagination as PaginatedResponse<{ ExceptionId: number }>
  );
  if (link) response.headers.set("Link", link);
}

export async function GET(request: Request) {
  const url = new URL(request.url);
  const { searchParams } = url;
  const exceptionId = searchParams.get("exceptionId");
  const sortBy = searchParams.get("sortBy");
  const sortOrder = searchParams.get("sortOrder");
  const exceptionStatus = searchParams.get("exceptionStatus");
  const isReport = searchParams.get("isReport");
  const exceptionCategory = searchParams.get("exceptionCategory");
  const reportDate = searchParams.get("reportDate");
  const page = Math.max(1, Number.parseInt(searchParams.get("page") || "1", 10));

  // Handle single exception requests - get fresh data from store
  if (exceptionId !== null) {
    const id = Number(exceptionId);
    const exception = mockDataStore.getException(id);

    if (exception) {
      return NextResponse.json(exception, { status: 200 });
    }

    return NextResponse.json(
      { error: `Exception with ID ${exceptionId} not found` },
      { status: 404 }
    );
  }

  if (exceptionStatus !== null) {
    const isRaised = exceptionStatus === ExceptionStatus.Raised.toString();;
    const allItems = isRaised
      ? mockDataStore.getRaisedExceptions()
      : mockDataStore.getNotRaisedExceptions();

    const categoryFiltered = exceptionCategory
      ? allItems.filter((i) => i.Category === Number(exceptionCategory))
      : allItems;

    const sortedItems = sortExceptions([...categoryFiltered], sortBy, sortOrder);

    const withDetails = addExceptionDetails(sortedItems);
    const paginated = paginate(withDetails, page, PAGE_SIZE);
    const json = NextResponse.json(paginated, { status: 200 });
    addPaginationHeaders(json, url, paginated);
    return json;
  }

  // Handle report list requests if exceptionStatus is not specified
  if (isReport === "1" || isReport?.toLowerCase() === "true") {
    const usingSort = sortBy ?? sortOrder; // accept either param name
    // Report mode returns Confusion + Superseded by default, or a specific category if provided
    const all = [
      ...mockDataStore.getRaisedExceptions(),
      ...mockDataStore.getNotRaisedExceptions(),
    ];
    const reportCategories = new Set<number>([12, 13]); // Confusion=12, Superseded=13
    const byCategory = exceptionCategory
      ? all.filter((i) => i.Category === Number(exceptionCategory))
      : all.filter((i) => reportCategories.has(i.Category));

    let dateFiltered = byCategory;
    if (reportDate) {
      const prefix = `${reportDate}`;
      dateFiltered = byCategory.filter((i) =>
        i.DateCreated?.startsWith(prefix)
      );
    }

    const sorted = sortExceptions([...dateFiltered], usingSort);
    const withDetails = addExceptionDetails(sorted);
    const paginated = paginate(withDetails, page, PAGE_SIZE);
    const json = NextResponse.json(paginated, { status: 200 });
    addPaginationHeaders(json, url, paginated);
    return json;
  }

  {
    const usingSort = sortBy ?? sortOrder;
    const allItems = mockDataStore.getNotRaisedExceptions();
    const sortedItems = sortExceptions([...allItems], usingSort, "DateCreated");
    const withDetails = addExceptionDetails(sortedItems);
    const paginated = paginate(withDetails, page, PAGE_SIZE);
    const json = NextResponse.json(paginated, { status: 200 });
    addPaginationHeaders(json, url, paginated);
    return json;
  }
}
