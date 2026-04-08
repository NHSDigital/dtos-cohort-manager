import Link from "next/link";

interface CardProps {
  readonly value?: number;
  readonly label: string;
  readonly description?: string;
  readonly url: string;
  readonly loading?: boolean;
}

export default function Card({
  value,
  label,
  description,
  url,
  loading = false,
}: Readonly<CardProps>) {
  const hasValue = value !== undefined;
  const isClickable = !loading && (!hasValue || value > 0);
  return (
    <div
      className={`nhsuk-card${isClickable ? " nhsuk-card--clickable" : ""}`}
      data-testid="card"
    >
      <div className="nhsuk-card__content">
        {loading ? (
          <p className="nhsuk-heading-xl nhsuk-u-font-size-64 nhsuk-u-margin-bottom-1" data-testid="card-number">
            <span className="app-loading-shimmer" aria-hidden="true">&mdash;</span>
            <span className="nhsuk-u-visually-hidden">Loading {label}</span>
          </p>
        ) : hasValue && (
          <p className="nhsuk-heading-xl nhsuk-u-font-size-64 nhsuk-u-margin-bottom-1" data-testid="card-number">
            {value} <span className="nhsuk-u-visually-hidden">{label}</span>
          </p>
        )}
        <h3 className="nhsuk-card__heading nhsuk-heading-m" data-testid="card-heading">
          {isClickable ? (
            <Link href={url} className="nhsuk-link--no-visited-state">
              {label}
            </Link>
          ) : (
            label
          )}
        </h3>
        {description && (
          <p className="nhsuk-card__description" data-testid="card-description">
            {description}
          </p>
        )}
      </div>
    </div>
  );
}
