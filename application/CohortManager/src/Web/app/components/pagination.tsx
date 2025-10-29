interface PaginationItem {
  readonly number: number;
  readonly href: string;
  readonly current?: boolean;
}

interface PaginationProps {
  readonly linkHeader?: string | null;
  readonly currentPage: number;
  readonly totalPages: number;
  readonly buildUrl: (page: number) => string;
}

function parseLinkHeader(linkHeader: string): {
  first?: string;
  previous?: string;
  next?: string;
  last?: string;
} {
  const links: {
    first?: string;
    previous?: string;
    next?: string;
    last?: string;
  } = {};

  if (!linkHeader) return links;

  const linkRegex = /<([^>]+)>;\s*rel="([^"]+)"/g;
  let match;

  while ((match = linkRegex.exec(linkHeader)) !== null) {
    const url = match[1];
    const rel = match[2];

    if (rel === "first") links.first = url;
    if (rel === "prev" || rel === "previous") links.previous = url;
    if (rel === "next") links.next = url;
    if (rel === "last") links.last = url;
  }

  return links;
}

function generatePaginationItems(
  currentPage: number,
  totalPages: number,
  buildUrl: (page: number) => string,
  maxVisiblePages: number = 10
): PaginationItem[] {
  const items: PaginationItem[] = [];

  if (totalPages <= maxVisiblePages) {
    for (let i = 1; i <= totalPages; i++) {
      items.push({
        number: i,
        href: buildUrl(i),
        current: i === currentPage,
      });
    }
    return items;
  }

  let startPage = Math.max(1, currentPage - 3);
  let endPage = Math.min(totalPages, currentPage + 3);

  if (currentPage <= 4) {
    endPage = Math.min(maxVisiblePages, totalPages);
  }

  if (currentPage >= totalPages - 3) {
    startPage = Math.max(1, totalPages - maxVisiblePages + 1);
  }

  if (startPage > 1) {
    items.push({
      number: 1,
      href: buildUrl(1),
      current: false,
    });

    if (startPage > 2) {
      items.push({
        number: -1,
        href: "#",
        current: false,
      });
    }
  }

  for (let i = startPage; i <= endPage; i++) {
    items.push({
      number: i,
      href: buildUrl(i),
      current: i === currentPage,
    });
  }

  if (endPage < totalPages) {
    if (endPage < totalPages - 1) {
      items.push({
        number: -1,
        href: "#",
        current: false,
      });
    }
    items.push({
      number: totalPages,
      href: buildUrl(totalPages),
      current: false,
    });
  }

  return items;
}

export default function Pagination({
  linkHeader,
  currentPage,
  totalPages,
  buildUrl,
}: PaginationProps) {
  const paginationLinks = parseLinkHeader(linkHeader || "");
  const items = generatePaginationItems(currentPage, totalPages, buildUrl);

  const previous = paginationLinks.previous
    ? { href: buildUrl(currentPage - 1) }
    : undefined;

  const next = paginationLinks.next
    ? { href: buildUrl(currentPage + 1) }
    : undefined;
  return (
    <nav
      className="app-pagination"
      aria-label="Pagination"
      data-testid="pagination"
    >
      {previous && (
        <div className="app-pagination__prev">
          <a
            className="app-link app-pagination__link"
            href={previous.href}
            rel="prev"
          >
            <svg
              className="app-pagination__icon app-pagination__icon--prev"
              xmlns="http://www.w3.org/2000/svg"
              height="13"
              width="15"
              aria-hidden="true"
              focusable="false"
              viewBox="0 0 15 13"
            >
              <path d="m6.5938-0.0078125-6.7266 6.7266 6.7441 6.4062 1.377-1.449-4.1856-3.9768h12.896v-2h-12.984l4.2931-4.293-1.414-1.414z"></path>
            </svg>
            <span className="app-pagination__link-title">
              Previous<span className="nhsuk-u-visually-hidden"> page</span>
            </span>
          </a>
        </div>
      )}
      <ul className="app-pagination__list">
        {items.map((item, index) => (
          <li
            key={item.number === -1 ? `ellipsis-${index}` : item.number}
            className={`app-pagination__item${
              item.current ? " app-pagination__item--current" : ""
            }`}
          >
            {item.number === -1 ? (
              <span className="app-pagination__ellipsis" aria-hidden="true">
                ...
              </span>
            ) : (
              <a
                className="app-link app-pagination__link"
                href={item.href}
                aria-label={`Page ${item.number}`}
                {...(item.current && { "aria-current": "page" })}
              >
                {item.number}
              </a>
            )}
          </li>
        ))}
      </ul>
      {next && (
        <div className="app-pagination__next">
          <a
            className="app-link app-pagination__link"
            href={next.href}
            rel="next"
          >
            <span className="app-pagination__link-title">
              Next<span className="nhsuk-u-visually-hidden"> page</span>
            </span>
            <svg
              className="app-pagination__icon app-pagination__icon--next"
              xmlns="http://www.w3.org/2000/svg"
              height="13"
              width="15"
              aria-hidden="true"
              focusable="false"
              viewBox="0 0 15 13"
            >
              <path d="m8.107-0.0078125-1.4136 1.414 4.2926 4.293h-12.986v2h12.896l-4.1855 3.9766 1.377 1.4492 6.7441-6.4062-6.7246-6.7266z"></path>
            </svg>
          </a>
        </div>
      )}
    </nav>
  );
}
