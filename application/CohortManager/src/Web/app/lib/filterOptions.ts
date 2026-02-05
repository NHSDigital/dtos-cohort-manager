import { ruleIdMappings } from "./ruleMapping";

export interface FilterOption {
  readonly value: string;
  readonly label: string;
}

export function getRuleFilterOptions(): FilterOption[] {
  return Object.entries(ruleIdMappings)
    .map(([ruleId, mapping]) => ({
      value: ruleId,
      label: mapping.ruleDescription || "",
    }))
    .filter((option) => option.label.length > 0)
    .sort((a, b) => a.label.localeCompare(b.label));
}

export function validateDateFilter(
  day?: string,
  month?: string,
  year?: string
): { isValid: boolean; error?: string; formattedDate?: string } {
  // If all fields are empty, that's valid (no filter)
  if (!day && !month && !year) {
    return { isValid: true };
  }

  // If any field is filled, all must be filled
  if (!day || !month || !year) {
    return { isValid: false, error: "Enter a complete date" };
  }

  const dayNum = Number.parseInt(day, 10);
  const monthNum = Number.parseInt(month, 10);
  const yearNum = Number.parseInt(year, 10);

  // Basic validation
  if (Number.isNaN(dayNum) || Number.isNaN(monthNum) || Number.isNaN(yearNum)) {
    return { isValid: false, error: "Enter a valid date" };
  }

  if (dayNum < 1 || dayNum > 31) {
    return { isValid: false, error: "Day must be between 1 and 31" };
  }

  if (monthNum < 1 || monthNum > 12) {
    return { isValid: false, error: "Month must be between 1 and 12" };
  }

  if (yearNum < 1900 || yearNum > new Date().getFullYear()) {
    return { isValid: false, error: "Enter a valid year" };
  }

  // Check if it's a valid date
  const date = new Date(yearNum, monthNum - 1, dayNum);
  if (
    date.getDate() !== dayNum ||
    date.getMonth() !== monthNum - 1 ||
    date.getFullYear() !== yearNum
  ) {
    return { isValid: false, error: "Enter a valid date" };
  }

  // Check if date is not in the future
  if (date > new Date()) {
    return { isValid: false, error: "Date cannot be in the future" };
  }

  // Format as YYYY-MM-DD for API
  const formattedDate = `${yearNum}-${String(monthNum).padStart(2, "0")}-${String(dayNum).padStart(2, "0")}`;

  return { isValid: true, formattedDate };
}
