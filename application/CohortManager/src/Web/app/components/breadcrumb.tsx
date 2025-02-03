import Link from "next/link";

interface BreadcrumbItem {
  readonly label: string;
  readonly url: string;
}

interface BreadcrumbProps {
  readonly items: readonly BreadcrumbItem[];
}

export default function Breadcrumb({ items }: Readonly<BreadcrumbProps>) {
  return (
    <nav className="nhsuk-breadcrumb" aria-label="Breadcrumb">
      <ol className="nhsuk-breadcrumb__list">
        {items.map((item, index) => (
          <li key={index} className="nhsuk-breadcrumb__item">
            <Link className="nhsuk-breadcrumb__link" href={item.url}>
              {item.label}
            </Link>
          </li>
        ))}
      </ol>
      {items.length > 0 && (
        <p className="nhsuk-breadcrumb__back">
          <Link className="nhsuk-breadcrumb__backlink" href={items[0].url}>
            <span className="nhsuk-u-visually-hidden">Back to &nbsp;</span>
            {items[0].label}
          </Link>
        </p>
      )}
    </nav>
  );
}
