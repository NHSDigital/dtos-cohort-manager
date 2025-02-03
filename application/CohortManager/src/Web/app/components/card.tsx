import Link from "next/link";

interface CardProps {
  value: number;
  label: string;
  url: string;
}

export default function Card({ value, label, url }: CardProps) {
  return (
    <div className="nhsuk-card nhsuk-card--clickable">
      <div className="nhsuk-card__content">
        <p className="nhsuk-heading-xl nhsuk-u-font-size-64 nhsuk-u-margin-bottom-1">
          {value} <span className="nhsuk-u-visually-hidden">{label}</span>
        </p>
        <Link
          href={url}
          className="nhsuk-card__link nhsuk-u-font-weight-normal nhsuk-u-font-size-19 nhsuk-link--no-visited-state"
        >
          {label}
        </Link>
      </div>
    </div>
  );
}
