import Card from "@/app/components/card";

interface CardProps {
  readonly value: number;
  readonly label: string;
  readonly url: string;
}

interface CardGroupProps {
  readonly items: readonly CardProps[];
}

export default function CardGroup({ items }: Readonly<CardGroupProps>) {
  return (
    <ul className="nhsuk-grid-row nhsuk-card-group">
      {items.map((card, index) => (
        <li
          className="nhsuk-grid-column-one-third nhsuk-card-group__item"
          key={index}
        >
          <Card value={card.value} label={card.label} url={card.url} />
        </li>
      ))}
    </ul>
  );
}
