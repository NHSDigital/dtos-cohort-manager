export const SearchType = {
  NhsNumber: 0,
  ExceptionId: 1,
} as const;

export type SearchTypeValue = (typeof SearchType)[keyof typeof SearchType];

export const SearchTypeLabels: Record<SearchTypeValue, string> = {
  [SearchType.NhsNumber]: "NHS Number",
  [SearchType.ExceptionId]: "Exception ID",
};

export const SearchTypePlaceholders: Record<SearchTypeValue, string> = {
  [SearchType.NhsNumber]: "Search by NHS Number",
  [SearchType.ExceptionId]: "Search by Exception ID",
};
