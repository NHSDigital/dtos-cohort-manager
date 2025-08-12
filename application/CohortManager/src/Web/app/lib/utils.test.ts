import {
  formatNhsNumber,
  formatDate,
  formatCompactDate,
  getCurrentDate,
  formatGenderValue,
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

describe("formatGenderValue", () => {
  it("should return 'Male' for input '1' (string)", () => {
    expect(formatGenderValue("1")).toBe("Male");
  });

  it("should return 'Male' for input 1 (number)", () => {
    expect(formatGenderValue(1)).toBe("Male");
  });

  it("should return 'Female' for input '2' (string)", () => {
    expect(formatGenderValue("2")).toBe("Female");
  });

  it("should return 'Female' for input 2 (number)", () => {
    expect(formatGenderValue(2)).toBe("Female");
  });

  it("should return 'Unspecified' for input '9' (string)", () => {
    expect(formatGenderValue("9")).toBe("Unspecified");
  });

  it("should return 'Unspecified' for input 9 (number)", () => {
    expect(formatGenderValue(9)).toBe("Unspecified");
  });

  it("should return 'Unknown' for input '3' (string)", () => {
    expect(formatGenderValue("3")).toBe("Unknown");
  });

  it("should return 'Unknown' for input 3 (number)", () => {
    expect(formatGenderValue(3)).toBe("Unknown");
  });

  it("should return 'Unknown' for undefined input", () => {
    expect(formatGenderValue(undefined)).toBe("Unknown");
  });

  it("should return 'Unknown' for null input", () => {
    expect(formatGenderValue(null)).toBe("Unknown");
  });

  it("should return 'Unknown' for empty string input", () => {
    expect(formatGenderValue("")).toBe("Unknown");
  });
});
