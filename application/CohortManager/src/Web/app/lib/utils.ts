export const formatNhsNumber = (nhsNumber: string): string => {
  return nhsNumber.replace(/(\d{3})(\d{3})(\d{4})/, "$1 $2 $3");
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
  const year = dateString.slice(0, 4);
  const month = dateString.slice(4, 6);
  const day = dateString.slice(6, 8);
  const date = new Date(`${year}-${month}-${day}`);
  const options: Intl.DateTimeFormatOptions = {
    year: "numeric",
    month: "long",
    day: "numeric",
  };
  return date.toLocaleDateString("en-GB", options);
};

export const formatPhoneNumber = (phoneNumber: string): string => {
  return phoneNumber.replace(/(\d{5})(\d{3})(\d{3})/, "$1 $2 $3");
};

export function formatCIS2Roles(roles?: string): string[] {
  if (!roles) return [];
  return roles.replace(/"/g, "").split(":");
}

export function getCurrentDate(): string {
  const date = new Date();
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const day = String(date.getDate()).padStart(2, "0");
  return `${year}-${month}-${day}`;
}
