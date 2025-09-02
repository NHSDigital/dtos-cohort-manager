interface PaginationItem {
  readonly number: number;
  readonly href: string;
  readonly current?: boolean;
}

interface PaginationLink {
  readonly href: string;
}

interface PaginationProps {
  readonly items: readonly PaginationItem[];
  readonly previous?: PaginationLink;
  readonly next?: PaginationLink;
}

export default function Pagination({ items, previous, next }: PaginationProps) {
  return (
    <nav className="app-pagination" aria-label="Pagination">
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
        {items.map((item) => (
          <li
            key={item.number}
            className={`app-pagination__item${
              item.current ? " app-pagination__item--current" : ""
            }`}
          >
            <a
              className="app-link app-pagination__link"
              href={item.href}
              aria-label={`Page ${item.number}`}
              {...(item.current && { "aria-current": "page" })}
            >
              {item.number}
            </a>
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
