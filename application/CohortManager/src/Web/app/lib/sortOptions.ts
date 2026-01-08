export enum SortBy {
  DateCreated = 0,
  NhsNumber = 1,
  ExceptionId = 2,
}

export enum SortOrder {
  Ascending = 0,
  Descending = 1,
}

export interface SortOption {
  value: string;
  label: string;
  sortBy: SortBy;
  sortOrder: SortOrder;
}

export const SortOptions: SortOption[] = [
  {
    value: "0",
    label: "Date exception created (oldest first)",
    sortBy: SortBy.DateCreated,
    sortOrder: SortOrder.Ascending,
  },
  {
    value: "1",
    label: "Date exception created (newest first)",
    sortBy: SortBy.DateCreated,
    sortOrder: SortOrder.Descending,
  },
  {
    value: "2",
    label: "Exception ID (ascending)",
    sortBy: SortBy.ExceptionId,
    sortOrder: SortOrder.Ascending,
  },
  {
    value: "3",
    label: "Exception ID (descending)",
    sortBy: SortBy.ExceptionId,
    sortOrder: SortOrder.Descending,
  },
  {
    value: "4",
    label: "NHS Number (ascending)",
    sortBy: SortBy.NhsNumber,
    sortOrder: SortOrder.Ascending,
  },
  {
    value: "5",
    label: "NHS Number (descending)",
    sortBy: SortBy.NhsNumber,
    sortOrder: SortOrder.Descending,
  },
];

export function getSortOption(value: string): SortOption {
  const option = SortOptions.find(opt => opt.value === value);
  return option || SortOptions[1];
}
