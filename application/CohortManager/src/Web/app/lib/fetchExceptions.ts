"use server";

export async function fetchExceptions(exceptionId?: number) {
  const apiUrl = exceptionId
    ? `${process.env.EXCEPTIONS_API_URL}/api/GetValidationExceptions?exceptionId=${exceptionId}`
    : `${process.env.EXCEPTIONS_API_URL}/api/GetValidationExceptions`;

  const response = await fetch(apiUrl);

  if (response.status === 204) {
    return [];
  }

  if (!response.ok) {
    throw new Error(`Error fetching data: ${response.statusText}`);
  }
  return response.json();
}

export async function fetchExceptionsNotRaised() {
  const apiUrl = `${process.env.EXCEPTIONS_API_URL}/api/GetValidationExceptions?exceptionStatus=2`;

  const response = await fetch(apiUrl);

  if (response.status === 204) {
    return [];
  }

  if (!response.ok) {
    throw new Error(`Error fetching data: ${response.statusText}`);
  }
  return response.json();
}

export async function fetchExceptionsRaised() {
  const apiUrl = `${process.env.EXCEPTIONS_API_URL}/api/GetValidationExceptions?exceptionStatus=1`;

  const response = await fetch(apiUrl);

  if (response.status === 204) {
    return [];
  }

  if (!response.ok) {
    throw new Error(`Error fetching data: ${response.statusText}`);
  }
  return response.json();
}

export async function fetchExceptionsNotRaisedSorted(sortOrder: 0 | 1) {
  const apiUrl = `${process.env.EXCEPTIONS_API_URL}/api/GetValidationExceptions?exceptionStatus=2&sortOrder=${sortOrder}`;

  const response = await fetch(apiUrl);

  if (response.status === 204) {
    return [];
  }

  if (!response.ok) {
    throw new Error(`Error fetching data: ${response.statusText}`);
  }
  return response.json();
}

export async function fetchExceptionsRaisedSorted(sortOrder: 0 | 1) {
  const apiUrl = `${process.env.EXCEPTIONS_API_URL}/api/GetValidationExceptions?exceptionStatus=1&sortOrder=${sortOrder}`;

  const response = await fetch(apiUrl);

  if (response.status === 204) {
    return [];
  }

  if (!response.ok) {
    throw new Error(`Error fetching data: ${response.statusText}`);
  }
  return response.json();
}
