export const formatNhsNumber = (nhsNumber: string | number): string => {
  if (!nhsNumber) return '';
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

  let year: string, month: string, day: string;

  if (dateString.includes('-')) {
    const parts = dateString.split('-');
    if (parts.length !== 3) return "";
    [year, month, day] = parts;
  } else {
    if (dateString.length < 8) return "";
    year = dateString.slice(0, 4);
    month = dateString.slice(4, 6);
    day = dateString.slice(6, 8);
  }

  const date = new Date(`${year}-${month}-${day}`);
  if (isNaN(date.getTime())) return "";
  const options: Intl.DateTimeFormatOptions = {
    year: "numeric",
    month: "long",
    day: "numeric",
  };
  return date.toLocaleDateString("en-GB", options);
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
