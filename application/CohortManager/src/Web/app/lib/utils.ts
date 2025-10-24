export const formatNhsNumber = (nhsNumber: string | number): string => {
  if (!nhsNumber) return "";
  const nhsString = String(nhsNumber);
  return nhsString.replace(/(\d{3})(\d{3})(\d{4})/, "$1 $2 $3");
};

export const formatDate = (dateString: string): string => {
  const date = new Date(dateString);
  const options: Intl.DateTimeFormatOptions = {
    year: "numeric",
    month: "long",
    day: "numeric",
  };
  return date.toLocaleDateString("en-GB", options);
};

export const formatCompactDate = (dateString: string): string => {
  if (!dateString) return "";

  const date = new Date(dateString);
  if (isNaN(date.getTime())) return "";

  const day = String(date.getDate()).padStart(2, "0");
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const year = date.getFullYear();

  return `${day}/${month}/${year}`;
};

export function getCurrentDate(): string {
  const date = new Date();
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const day = String(date.getDate()).padStart(2, "0");
  return `${year}-${month}-${day}`;
}

export function formatGenderValue(gender?: number | string | null): string {
  if (gender === null || gender === undefined || gender === "") return "";
  const genderNum = typeof gender === "string" ? parseInt(gender, 10) : gender;
  if (genderNum === 1) return "Male";
  if (genderNum === 2) return "Female";
  if (genderNum === 9) return "Unspecified";
  return "";
}

export const formatIsoDate = (date: Date): string => {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const day = String(date.getDate()).padStart(2, "0");
  return `${year}-${month}-${day}`;
};
