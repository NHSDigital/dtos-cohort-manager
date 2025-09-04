import {
  parseLinkHeader,
  extractPageFromUrl,
  convertToLocalUrl,
  generatePaginationItems,
  type LinkBasedPagination,
} from "./pagination";

describe("pagination utilities", () => {
  test("parseLinkHeader parses standard RFC5988 link header", () => {
    const header =
      '<https://api.example.com/items?page=1>; rel="first", ' +
      '<https://api.example.com/items?page=2>; rel="prev", ' +
      '<https://api.example.com/items?page=4>; rel="next", ' +
      '<https://api.example.com/items?page=10>; rel="last"';

    const links = parseLinkHeader(header);
    expect(links.first).toBe("https://api.example.com/items?page=1");
    expect(links.previous).toBe("https://api.example.com/items?page=2");
    expect(links.next).toBe("https://api.example.com/items?page=4");
    expect(links.last).toBe("https://api.example.com/items?page=10");
  });

  test("extractPageFromUrl returns page number or 1 by default", () => {
    expect(extractPageFromUrl("https://x.com?a=1&page=7&b=2")).toBe(7);
    expect(extractPageFromUrl("https://x.com" as unknown as string)).toBe(1);
  });

  test("convertToLocalUrl keeps page and adds sortBy", () => {
    expect(convertToLocalUrl("https://x.com?page=3", 1)).toBe(
      "?sortBy=1&page=3"
    );
    expect(convertToLocalUrl("https://x.com", 0)).toBe("?sortBy=0&page=1");
  });

  test("generatePaginationItems returns all pages when under threshold", () => {
    const pagination: LinkBasedPagination = {
      links: {},
      currentPage: 2,
      totalPages: 5,
    };

    const items = generatePaginationItems(pagination, 0);
    expect(items).toHaveLength(5);
    expect(items[1].current).toBe(true); // page 2 current
    expect(items[1].href).toBe("?sortBy=0&page=2");
  });

  test("generatePaginationItems truncates and includes ellipses when needed", () => {
    const pagination: LinkBasedPagination = {
      links: {},
      currentPage: 6,
      totalPages: 20,
    };

    const items = generatePaginationItems(pagination, 1);
    // Expect an ellipsis represented by number -1 somewhere
    expect(items.some((i) => i.number === -1)).toBe(true);
    // Current page should be marked
    expect(items.some((i) => i.number === 6 && i.current)).toBe(true);
  });
});
