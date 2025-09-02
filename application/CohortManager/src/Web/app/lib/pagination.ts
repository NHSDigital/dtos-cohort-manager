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
