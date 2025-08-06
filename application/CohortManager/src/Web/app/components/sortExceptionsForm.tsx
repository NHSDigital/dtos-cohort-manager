interface SortExceptionsFormProps {
  readonly sortBy: number;
  readonly options: readonly {
    readonly value: string;
    readonly label: string;
  }[];
  readonly hiddenText?: string;
  readonly testId?: string;
}

export default function SortExceptionsForm({
  sortBy,
  options,
  hiddenText = "exceptions",
  testId,
}: SortExceptionsFormProps) {
  return (
    <form method="GET">
      <div className="nhsuk-form-group app-form-group--inline">
        <label className="nhsuk-label" htmlFor="sort-exceptions">
          Sort <span className="nhsuk-u-visually-hidden">{hiddenText} </span> by
        </label>
        <div className="form-inline-row">
          <select
            className="nhsuk-select"
            id="sort-exceptions"
            name="sortBy"
            defaultValue={String(sortBy)}
            data-testid={testId}
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
      </div>
    </form>
  );
}
