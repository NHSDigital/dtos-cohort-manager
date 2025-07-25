import Card from "@/app/components/card";

interface CardProps {
  readonly value: number;
  readonly label: string;
  readonly description?: string;
  readonly url: string;
}

interface CardGroupProps {
  readonly items: readonly CardProps[];
}

export default function CardGroup({ items }: Readonly<CardGroupProps>) {
  return (
    <ul className="nhsuk-grid-row nhsuk-card-group">
      {items.map((card) => (
        <li
          className="nhsuk-grid-column-one-quarter nhsuk-card-group__item"
          key={card.label}
        >
          <Card
            value={card.value}
            label={card.label}
            description={card.description}
            url={card.url}
          />
        </li>
      ))}
    </ul>
  );
}
