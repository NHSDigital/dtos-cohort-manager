export interface PaginationLinks {
  first?: string;
  previous?: string;
  next?: string;
  last?: string;
}

export interface LinkBasedPagination {
  links: PaginationLinks;
  currentPage: number;
  totalPages: number;
}

export interface PaginationItem {
  number: number;
  href: string;
  current: boolean;
}

export function parseLinkHeader(linkHeader: string): PaginationLinks {
  const links: PaginationLinks = {};

  if (!linkHeader) return links;

  const linkPattern = /<([^>]+)>;\s*rel="([^"]+)"/g;
  let match: RegExpExecArray | null;

  while ((match = linkPattern.exec(linkHeader)) !== null) {
    const url = match[1];
    const relation = match[2];

    switch (relation.toLowerCase()) {
      case "first":
        links.first = url;
        break;
      case "prev":
      case "previous":
        links.previous = url;
        break;
      case "next":
        links.next = url;
        break;
      case "last":
        links.last = url;
        break;
    }
  }

  return links;
}

export function extractPageFromUrl(url: string): number {
  const pageRegex = /[?&]page=(\d+)/;
  const match = pageRegex.exec(url);
  return match ? parseInt(match[1], 10) : 1;
}

export function convertToLocalUrl(
  url: string | undefined,
  sortBy: number
): string | undefined {
  if (!url) return undefined;

  const pageRegex = /[?&]page=(\d+)/;
  const pageMatch = pageRegex.exec(url);
  const page = pageMatch ? pageMatch[1] : "1";

  return `?sortBy=${sortBy}&page=${page}`;
}

function createPaginationItem(
  pageNumber: number,
  sortBy: number,
  currentPage: number,
  isEllipsis = false
): PaginationItem {
  return {
    number: isEllipsis ? -1 : pageNumber,
    href: isEllipsis ? "#" : `?sortBy=${sortBy}&page=${pageNumber}`,
    current: pageNumber === currentPage,
  };
}

function generateAllPages(
  totalPages: number,
  sortBy: number,
  currentPage: number
): PaginationItem[] {
  const items: PaginationItem[] = [];
  for (let i = 1; i <= totalPages; i++) {
    items.push(createPaginationItem(i, sortBy, currentPage));
  }
  return items;
}

function calculatePageRange(
  currentPage: number,
  totalPages: number,
  maxVisiblePages: number
): { startPage: number; endPage: number } {
  let startPage = Math.max(1, currentPage - 3);
  let endPage = Math.min(totalPages, currentPage + 3);

  if (currentPage <= 4) {
    endPage = Math.min(maxVisiblePages, totalPages);
  } else if (currentPage >= totalPages - 3) {
    startPage = Math.max(1, totalPages - maxVisiblePages + 1);
  }

  return { startPage, endPage };
}

function addFirstPageSection(
  items: PaginationItem[],
  startPage: number,
  sortBy: number,
  currentPage: number
): void {
  if (startPage > 1) {
    items.push(createPaginationItem(1, sortBy, currentPage));

    if (startPage > 2) {
      items.push(createPaginationItem(-1, sortBy, currentPage, true));
    }
  }
}

function addVisiblePages(
  items: PaginationItem[],
  startPage: number,
  endPage: number,
  sortBy: number,
  currentPage: number
): void {
  for (let i = startPage; i <= endPage; i++) {
    items.push(createPaginationItem(i, sortBy, currentPage));
  }
}

function addLastPageSection(
  items: PaginationItem[],
  endPage: number,
  totalPages: number,
  sortBy: number,
  currentPage: number
): void {
  if (endPage < totalPages) {
    if (endPage < totalPages - 1) {
      items.push(createPaginationItem(-1, sortBy, currentPage, true));
    }
    items.push(createPaginationItem(totalPages, sortBy, currentPage));
  }
}

function generateTruncatedPages(
  currentPage: number,
  totalPages: number,
  sortBy: number,
  maxVisiblePages: number
): PaginationItem[] {
  const items: PaginationItem[] = [];
  const { startPage, endPage } = calculatePageRange(
    currentPage,
    totalPages,
    maxVisiblePages
  );

  addFirstPageSection(items, startPage, sortBy, currentPage);
  addVisiblePages(items, startPage, endPage, sortBy, currentPage);
  addLastPageSection(items, endPage, totalPages, sortBy, currentPage);

  return items;
}

export function generatePaginationItems(
  linkPagination: LinkBasedPagination,
  sortBy: number
): PaginationItem[] {
  const { currentPage, totalPages } = linkPagination;
  const maxVisiblePages = 10;

  if (totalPages <= maxVisiblePages) {
    return generateAllPages(totalPages, sortBy, currentPage);
  }

  return generateTruncatedPages(
    currentPage,
    totalPages,
    sortBy,
    maxVisiblePages
  );
}
/**
 * Creates a URL for pagination with preserved query parameters
 * @param page - The page number to navigate to
 * @param sortBy - The current sort option (0 or 1)
 * @param basePath - The base path for the URL (default: '/exceptions')
 * @returns The formatted URL with query parameters
 */
export function createPageUrl(
  page: number,
  sortBy: number = 0,
  basePath: string = "/exceptions"
): string {
  const params = new URLSearchParams();

  if (sortBy !== 0) {
    params.set("sortBy", String(sortBy));
  }

  if (page !== 1) {
    params.set("page", String(page));
  }

  const query = params.toString();
  return `${basePath}${query ? `?${query}` : ""}`;
}
