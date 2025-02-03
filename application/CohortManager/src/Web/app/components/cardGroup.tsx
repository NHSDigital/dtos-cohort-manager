import Card from "@/app/components/card";

interface CardProps {
  value: number;
  label: string;
  url: string;
}

interface CardGroupProps {
  items: CardProps[];
}

export default function CardGroup({ items }: CardGroupProps) {
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
