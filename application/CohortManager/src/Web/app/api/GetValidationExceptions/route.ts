import { NextResponse } from "next/server";
import mockDataStore from "@/app/data/mockDataStore";

function sortExceptions<
  T extends { DateCreated: string; ServiceNowCreatedDate?: string }
>(
  items: T[],
  sortOrder: string | null,
  dateField: keyof T = "DateCreated"
): T[] {
  if (sortOrder === "1") {
    return items.sort(
      (a, b) =>
        new Date(a[dateField] as string).getTime() -
        new Date(b[dateField] as string).getTime()
    );
  } else if (sortOrder === "0") {
    return items.sort(
      (a, b) =>
        new Date(b[dateField] as string).getTime() -
        new Date(a[dateField] as string).getTime()
    );
  }
  return items;
}

function createExceptionListResponse<T extends { ExceptionId: number }>(
  items: T[]
) {
  return {
    Items: items,
    IsFirstPage: true,
    HasNextPage: false,
    LastResultId: items[items.length - 1]?.ExceptionId ?? null,
    TotalItems: items.length,
    TotalPages: 1,
    CurrentPage: 1,
  };
}

function addExceptionDetails<T extends { ExceptionId: number }>(items: T[]) {
  const exceptions = mockDataStore.getExceptions();
  return items.map((item) => ({
    ...item,
    ExceptionDetails: exceptions[item.ExceptionId]?.ExceptionDetails ?? null,
  }));
}

export async function GET(request: Request) {
  const { searchParams } = new URL(request.url);
  const exceptionId = searchParams.get("exceptionId");
  const sortOrder = searchParams.get("sortOrder");
  const exceptionStatus = searchParams.get("exceptionStatus");
  const isReport = searchParams.get("isReport");
  const exceptionCategory = searchParams.get("exceptionCategory");
  const reportDate = searchParams.get("reportDate");

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

  // Handle list requests - get fresh data from store
  if (exceptionStatus !== null) {
    const usingSort = sortOrder ?? sortOrder; // accept either param name
    const isRaised = exceptionStatus === "1";
    const allItems = isRaised
      ? mockDataStore.getRaisedExceptions()
      : mockDataStore.getNotRaisedExceptions();

    // Optional category filter (e.g., 12 or 13)
    const categoryFiltered = exceptionCategory
      ? allItems.filter((i) => i.Category === Number(exceptionCategory))
      : allItems;

    // Optional date filter for report mode
    // If isReport = 1 and a reportDate (YYYY-MM-DD) is provided, try to match on
    // ServiceNowCreatedDate for raised items, otherwise DateCreated for not raised.
    let dateFiltered = categoryFiltered;
    if (isReport === "1" && reportDate) {
      const datePrefix = `${reportDate}`; // already YYYY-MM-DD
      type DateField = "ServiceNowCreatedDate" | "DateCreated";
      const dateField: DateField = isRaised
        ? "ServiceNowCreatedDate"
        : "DateCreated";
      dateFiltered = categoryFiltered.filter((i) => {
        const value = i[dateField as keyof typeof i] as unknown as
          | string
          | undefined;
        return value ? value.startsWith(datePrefix) : false;
      });
    }

    // Sort: for raised items prefer ServiceNowCreatedDate, otherwise DateCreated
    const sortedItems = sortExceptions(
      [...dateFiltered],
      usingSort,
      isRaised ? "ServiceNowCreatedDate" : "DateCreated"
    );

    const response = createExceptionListResponse(
      addExceptionDetails(sortedItems)
    );
    return NextResponse.json(response, { status: 200 });
  }

  // Default fallback
  return NextResponse.json(
    { error: "No valid query parameters provided" },
    { status: 400 }
  );
}
