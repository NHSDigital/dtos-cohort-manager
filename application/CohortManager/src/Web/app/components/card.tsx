import Link from "next/link";

interface CardProps {
  readonly value: number;
  readonly label: string;
  readonly description?: string;
  readonly url: string;
}

export default function Card({
  value,
  label,
  description,
  url,
}: Readonly<CardProps>) {
  return (
    <div className="nhsuk-card nhsuk-card--clickable">
      <div className="nhsuk-card__content">
        <p className="nhsuk-heading-xl nhsuk-u-font-size-64 nhsuk-u-margin-bottom-1">
          {value} <span className="nhsuk-u-visually-hidden">{label}</span>
        </p>
        <h3 className="nhsuk-card__heading nhsuk-heading-m">
          <Link href={url} className="nhsuk-link--no-visited-state">
            {label}
          </Link>
        </h3>
        {description && (
          <p className="nhsuk-card__description">{description}</p>
        )}
      </div>
    </div>
  );
}
