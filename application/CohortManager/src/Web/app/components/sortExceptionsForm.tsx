import { SortOption } from "@/app/lib/sortOptions";

interface SortExceptionsFormProps {
  sortBy: string;
  options: SortOption[];
}

export default function SortExceptionsForm({
  sortBy,
  options,
}: Readonly<SortExceptionsFormProps>) {
  return (
    <form className="nhsuk-form">
      <div className="nhsuk-form-group">
        <label
          className="nhsuk-label nhsuk-u-visually-hidden"
          htmlFor="sort-exceptions"
        >
          Sort by
        </label>
        <select
          className="nhsuk-select"
          id="sort-exceptions"
          name="sortBy"
          defaultValue={sortBy}
        >
          {options.map((option) => (
            <option key={option.value} value={option.value}>
              {option.label}
            </option>
          ))}
        </select>
          <button
            className="nhsuk-button app-button--small"
            data-module="nhsuk-button"
            type="submit"
          >
            Apply
          </button>
      </div>
    </form>
  );
}
