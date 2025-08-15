import { NextResponse } from "next/server";
import mockDataStore from "@/app/data/mockDataStore";

function sortExceptions<
  T extends { DateCreated: string; ServiceNowCreatedDate?: string }
>(items: T[], sortBy: string | null, dateField: keyof T = "DateCreated"): T[] {
  if (sortBy === "1") {
    return items.sort(
      (a, b) =>
        new Date(a[dateField] as string).getTime() -
        new Date(b[dateField] as string).getTime()
    );
  } else if (sortBy === "0") {
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

export async function GET(request: Request) {
  const { searchParams } = new URL(request.url);
  const exceptionId = searchParams.get("exceptionId");
  const raisedOnly = searchParams.get("raisedOnly");
  const notRaisedOnly = searchParams.get("notRaisedOnly");
  const sortBy = searchParams.get("sortBy");

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
  if (notRaisedOnly) {
    const notRaisedExceptions = mockDataStore.getNotRaisedExceptions();
    const sortedItems = sortExceptions([...notRaisedExceptions], sortBy);
    const response = createExceptionListResponse(sortedItems);
    return NextResponse.json(response, { status: 200 });
  }

  if (raisedOnly) {
    const raisedExceptions = mockDataStore.getRaisedExceptions();
    const sortedItems = sortExceptions(
      [...raisedExceptions],
      sortBy,
      "ServiceNowCreatedDate"
    );
    const response = createExceptionListResponse(sortedItems);
    return NextResponse.json(response, { status: 200 });
  }

  // Default fallback
  return NextResponse.json(
    { error: "No valid query parameters provided" },
    { status: 400 }
  );
}
