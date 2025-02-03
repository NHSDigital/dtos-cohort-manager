import {
  formatNhsNumber,
  formatDate,
  formatCompactDate,
  formatPhoneNumber,
  formatCIS2Roles,
  getCurrentDate,
} from "@/app/lib/utils";

describe("formatNhsNumber", () => {
  it("should format the NHS number as XXX XXX XXXX", () => {
    const input = "1234567890";
    const expectedOutput = "123 456 7890";
    expect(formatNhsNumber(input)).toBe(expectedOutput);
  });

  it("should return the input if it is not a valid NHS number", () => {
    const input = "12345";
    const expectedOutput = "12345";
    expect(formatNhsNumber(input)).toBe(expectedOutput);
  });
});

describe("formatDate", () => {
  it("should format the date as 26 February 1993", () => {
    const input = "1993-02-26T11:53:01.243";
    const expectedOutput = "26 February 1993";
    expect(formatDate(input)).toBe(expectedOutput);
  });
});

describe("formatCompactDate", () => {
  it("should format the date as 26 February 1993", () => {
    const input = "19930226";
    const expectedOutput = "26 February 1993";
    expect(formatCompactDate(input)).toBe(expectedOutput);
  });
});

describe("formatPhoneNumber", () => {
  it("should format the phone number as XXXXX XXX XXX", () => {
    const input = "01619999999";
    const expectedOutput = "01619 999 999";
    expect(formatPhoneNumber(input)).toBe(expectedOutput);
  });

  it("should return the input if it is not a valid phone number", () => {
    const input = "12345";
    const expectedOutput = "12345";
    expect(formatPhoneNumber(input)).toBe(expectedOutput);
  });
});

describe("formatCIS2Roles", () => {
  it("should format the CIS2 roles as an array of strings", () => {
    const input = "role1:role2:role3";
    const expectedOutput = ["role1", "role2", "role3"];
    expect(formatCIS2Roles(input)).toEqual(expectedOutput);
  });

  it("should return an empty array if the input is an empty string", () => {
    const input = "";
    const expectedOutput: string[] = [];
    expect(formatCIS2Roles(input)).toEqual(expectedOutput);
  });

  it("should return an empty array if the input is undefined", () => {
    const input = undefined;
    const expectedOutput: string[] = [];
    expect(formatCIS2Roles(input)).toEqual(expectedOutput);
  });
});

describe("getCurrentDate", () => {
  it("should return the current date in the format YYYY-MM-DD", () => {
    const date = new Date();
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, "0");
    const day = String(date.getDate()).padStart(2, "0");
    const expectedOutput = `${year}-${month}-${day}`;
    expect(getCurrentDate()).toBe(expectedOutput);
  });
});
