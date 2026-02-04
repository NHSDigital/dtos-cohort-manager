import { SortOption } from "@/app/lib/sortOptions";

interface FilterOption {
  readonly value: string;
  readonly label: string;
}

interface FilterExceptionsFormProps {
  readonly ruleOptions: readonly FilterOption[];
  readonly selectedRuleId?: string;
  readonly dateDay?: string;
  readonly dateMonth?: string;
  readonly dateYear?: string;
  readonly sortBy: string;
  readonly sortOptions: readonly SortOption[];
  readonly page: string;
  readonly dateError?: string;
}

export default function FilterExceptionsForm({
  ruleOptions,
  selectedRuleId,
  dateDay,
  dateMonth,
  dateYear,
  sortBy,
  sortOptions,
  page,
  dateError,
}: Readonly<FilterExceptionsFormProps>) {
  const hasActiveFilters =
    selectedRuleId || (dateDay && dateMonth && dateYear);

  return (
    <form className="nhsuk-form" data-testid="filter-exceptions-form" method="GET">
      <div className="nhsuk-form-group">
        <label
          className="nhsuk-label"
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
          {sortOptions.map((option) => (
            <option key={option.value} value={option.value}>
              {option.label}
            </option>
          ))}
        </select>
      </div>

      <div className="nhsuk-form-group">
        <label className="nhsuk-label" htmlFor="filter-rule">
          Filter by exception type
        </label>
        <select
          className="nhsuk-select"
          id="filter-rule"
          name="ruleId"
          defaultValue={selectedRuleId || ""}
        >
          <option value="">All exception types</option>
          {ruleOptions.map((option) => (
            <option key={option.value} value={option.value}>
              {option.label}
            </option>
          ))}
        </select>
      </div>

      <div
        className={`nhsuk-form-group ${dateError ? "nhsuk-form-group--error" : ""}`}
      >
        <fieldset className="nhsuk-fieldset" role="group">
          <legend className="nhsuk-fieldset__legend">Filter by date</legend>
          {dateError && (
            <span className="nhsuk-error-message" id="date-created-error">
              <span className="nhsuk-u-visually-hidden">Error:</span>{" "}
              {dateError}
            </span>
          )}
          <div style={{ display: 'flex', alignItems: 'flex-end', gap: '1rem', flexWrap: 'wrap' }}>
            <div className="nhsuk-date-input" id="date-created" style={{ display: 'flex', flexWrap: 'wrap' }}>
              <div className="nhsuk-date-input__item">
                <div className="nhsuk-form-group">
                  <label
                    className="nhsuk-label nhsuk-date-input__label"
                    htmlFor="date-day"
                  >
                    Day
                  </label>
                  <input
                    className={`nhsuk-input nhsuk-date-input__input nhsuk-input--width-2 ${dateError ? "nhsuk-input--error" : ""}`}
                    id="date-day"
                    name="dateDay"
                    type="text"
                    inputMode="numeric"
                    defaultValue={dateDay}
                    maxLength={2}
                  />
                </div>
              </div>
              <div className="nhsuk-date-input__item">
                <div className="nhsuk-form-group">
                  <label
                    className="nhsuk-label nhsuk-date-input__label"
                    htmlFor="date-month"
                  >
                    Month
                  </label>
                  <input
                    className={`nhsuk-input nhsuk-date-input__input nhsuk-input--width-2 ${dateError ? "nhsuk-input--error" : ""}`}
                    id="date-month"
                    name="dateMonth"
                    type="text"
                    inputMode="numeric"
                    defaultValue={dateMonth}
                    maxLength={2}
                  />
                </div>
              </div>
              <div className="nhsuk-date-input__item">
                <div className="nhsuk-form-group">
                  <label
                    className="nhsuk-label nhsuk-date-input__label"
                    htmlFor="date-year"
                  >
                    Year
                  </label>
                  <input
                    className={`nhsuk-input nhsuk-date-input__input nhsuk-input--width-4 ${dateError ? "nhsuk-input--error" : ""}`}
                    id="date-year"
                    name="dateYear"
                    type="text"
                    inputMode="numeric"
                    defaultValue={dateYear}
                    maxLength={4}
                  />
                </div>
              </div>
            </div>
            <button
              className="nhsuk-button app-button--small"
              data-module="nhsuk-button"
              data-testid="apply-filters-button"
              type="submit"
              style={{ marginBottom: 0 }}
            >
              Apply
            </button>
            {hasActiveFilters && (
              <a
                href={`?sortBy=${sortBy}&page=1`}
                className="nhsuk-link"
                data-testid="clear-filters-link"
                style={{ marginBottom: '5px' }}
              >
                Clear filters
              </a>
            )}
          </div>
        </fieldset>
      </div>
    </form>
  );
}
