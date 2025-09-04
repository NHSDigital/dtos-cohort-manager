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

// Constants used across pagination helpers
const PAGE_PARAM = "page" as const;
const SORT_PARAM = "sortBy" as const;
const ELLIPSIS_NUMBER = -1 as const;
const DEFAULT_SORT = 0 as const;

export function parseLinkHeader(linkHeader: string): PaginationLinks {
  const links: PaginationLinks = {};

  if (!linkHeader) return links;

  // Defensive cap to avoid processing extremely large headers
  const MAX_LINK_HEADER_LENGTH = 16_384; // 16KB
  const header =
    linkHeader.length > MAX_LINK_HEADER_LENGTH
      ? linkHeader.slice(0, MAX_LINK_HEADER_LENGTH)
      : linkHeader;

  // Linear-time parsing without regex backtracking
  let i = 0;
  while (i < header.length) {
    // Find opening '<'
    const open = header.indexOf("<", i);
    if (open === -1) break;
    const close = header.indexOf(">", open + 1);
    if (close === -1) break; // malformed, stop parsing

    const url = header.slice(open + 1, close);

    // Determine the bounds of this link-value (until next comma or end)
    const comma = header.indexOf(",", close + 1);
    const segmentEnd = comma === -1 ? header.length : comma;
    const paramsSegment = header.slice(close + 1, segmentEnd);

    // Look for rel parameter in the params segment in a safe, non-regex way
    // Pattern of interest: rel="..." (case-insensitive for rel name)
    const relKeyIndex = paramsSegment.toLowerCase().indexOf("rel=");
    if (relKeyIndex !== -1) {
      const afterRel = relKeyIndex + 4; // position after 'rel='
      // Skip optional whitespace
      let p = afterRel;
      while (p < paramsSegment.length && /\s/.test(paramsSegment[p])) p++;

      let relation: string | null = null;
      if (p < paramsSegment.length && paramsSegment[p] === '"') {
        // Quoted form: rel="value"
        const start = p + 1;
        const end = paramsSegment.indexOf('"', start);
        relation = end !== -1 ? paramsSegment.slice(start, end) : null;
      } else {
        // Token form: rel=value; stop at ; or whitespace
        const semi = paramsSegment.indexOf(";", p);
        const space = paramsSegment.indexOf(" ", p);
        const stopCandidates = [
          semi === -1 ? paramsSegment.length : semi,
          space === -1 ? paramsSegment.length : space,
        ];
        const stop = Math.min(...stopCandidates);
        relation = paramsSegment.slice(p, stop).trim() || null;
      }

      if (relation) {
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
    }

    // Move to next segment (after comma if present)
    i = comma === -1 ? header.length : comma + 1;
  }

  return links;
}

export function extractPageFromUrl(url: string): number {
  // Prefer robust parsing via WHATWG URL; fall back to regex for non-absolute URLs
  try {
    const parsed = new URL(url);
    const value = parsed.searchParams.get(PAGE_PARAM);
    const page = value ? parseInt(value, 10) : 1;
    return Number.isNaN(page) ? 1 : Math.max(1, page);
  } catch {
    const pageRegex = new RegExp(`[?&]${PAGE_PARAM}=(\\d+)`);
    const match = pageRegex.exec(url);
    const page = match ? parseInt(match[1], 10) : 1;
    return Number.isNaN(page) ? 1 : Math.max(1, page);
  }
}

export function convertToLocalUrl(
  url: string | undefined,
  sortBy: number
): string | undefined {
  if (!url) return undefined;
  const page = extractPageFromUrl(url);
  // Return a relative query-string (no base path) and always include both params
  return createPageUrl(page, sortBy, "", { includeDefaultParams: true });
}

function createPaginationItem(
  pageNumber: number,
  sortBy: number,
  currentPage: number,
  isEllipsis = false
): PaginationItem {
  return {
    number: isEllipsis ? ELLIPSIS_NUMBER : pageNumber,
    href: isEllipsis
      ? "#"
      : createPageUrl(pageNumber, sortBy, "", { includeDefaultParams: true }),
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
  sortBy: number,
  options?: { maxVisiblePages?: number }
): PaginationItem[] {
  const { currentPage, totalPages } = linkPagination;
  const maxVisiblePages = options?.maxVisiblePages ?? 10;

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
  sortBy: number = DEFAULT_SORT,
  basePath: string = "/exceptions",
  opts?: { includeDefaultParams?: boolean }
): string {
  const params = new URLSearchParams();

  const includeDefaults = Boolean(opts?.includeDefaultParams);

  if (includeDefaults || sortBy !== DEFAULT_SORT) {
    params.set(SORT_PARAM, String(sortBy));
  }

  if (includeDefaults || page !== 1) {
    params.set(PAGE_PARAM, String(page));
  }

  const query = params.toString();
  return `${basePath}${query ? `?${query}` : ""}`;
}
