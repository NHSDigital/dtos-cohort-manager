import { NextRequest, NextResponse } from "next/server";
import mockDataStore from "@/app/data/mockDataStore";

interface UpdateExceptionRequest {
  ExceptionId: number | string;
  ServiceNowId?: string;
}
export async function PUT(request: NextRequest) {
  try {
    // 1. Read and validate request body
    let requestBody: string;
    try {
      requestBody = await request.text();
    } catch (error) {
      console.error("Error reading request body:", error);
      return NextResponse.json(
        { error: "Invalid request body" },
        { status: 400 }
      );
    }

    if (!requestBody || requestBody.trim() === "") {
      console.warn("Request body is empty.");
      return NextResponse.json(
        { error: "Request body cannot be empty." },
        { status: 400 }
      );
    }

    // 2. Deserialize request
    let updateRequest: UpdateExceptionRequest;
    try {
      updateRequest = JSON.parse(requestBody);
    } catch (error) {
      console.error("Error parsing JSON:", error);
      return NextResponse.json(
        { error: "Invalid JSON format" },
        { status: 400 }
      );
    }

    // 3. Validate ExceptionId
    if (updateRequest?.ExceptionId == null) {
      console.warn("ExceptionId is missing.");
      return NextResponse.json(
        { error: "ExceptionId is required" },
        { status: 400 }
      );
    }
    const rawExceptionId = updateRequest.ExceptionId;
    const exceptionId =
      typeof rawExceptionId === "number"
        ? rawExceptionId
        : Number.parseInt(rawExceptionId, 10);
    if (!Number.isFinite(exceptionId) || exceptionId <= 0) {
      console.warn("Invalid ExceptionId provided:", rawExceptionId);
      return NextResponse.json(
        { error: "Invalid ExceptionId provided" },
        { status: 400 }
      );
    }

    // 4. Check & Fetch Exception Record from shared data store
    const exceptionData = mockDataStore.getException(exceptionId);
    if (!exceptionData) {
      console.warn(`Exception with ID ${exceptionId} not found`);
      return NextResponse.json(
        { error: `Exception with ID ${exceptionId} not found` },
        { status: 404 }
      );
    }

    // 5. Update Exception Record with ServiceNow number using the data store
    const originalServiceNowId = exceptionData.ServiceNowId;
    const newServiceNowId = (updateRequest.ServiceNowId ?? "").trim();

    // Use the data store's update method to ensure consistency across both APIs
    const updateSuccess = mockDataStore.updateExceptionServiceNow(
      exceptionId,
      newServiceNowId
    );

    // 6. Check if update was successful and return appropriate response
    if (!updateSuccess) {
      console.error("Failed to update exception:", exceptionId);
      return NextResponse.json(
        { error: "Failed to update exception record" },
        { status: 500 }
      );
    }

    // Get the updated exception to return current values
    const updatedException = mockDataStore.getException(exceptionId);

    const changed = originalServiceNowId !== newServiceNowId;
    const message = changed
      ? "ServiceNowId updated successfully"
      : "ServiceNowId unchanged, but record timestamp updated";

    return NextResponse.json(
      {
        message,
        exceptionId: exceptionId,
        previousServiceNowId: originalServiceNowId,
        newServiceNowId: newServiceNowId,
        updatedAt: updatedException?.RecordUpdatedDate,
      },
      { status: 200 }
    );
  } catch (error) {
    console.error("An unexpected error occurred:", error);
    return NextResponse.json(
      { error: "An unexpected error occurred" },
      { status: 500 }
    );
  }
}
