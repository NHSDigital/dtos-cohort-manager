"use client";

import { useRouter, useSearchParams } from "next/navigation";
import { SortOption } from "@/app/lib/sortOptions";

interface SortExceptionsFormProps {
  sortBy: string;
  options: SortOption[];
}

export default function SortExceptionsForm({
  sortBy,
  options,
}: SortExceptionsFormProps) {
  const router = useRouter();
  const searchParams = useSearchParams();

  const handleSortChange = (event: React.ChangeEvent<HTMLSelectElement>) => {
    const newSortBy = event.target.value;
    const params = new URLSearchParams(searchParams);
    params.set("sortBy", newSortBy);
    params.delete("page"); // Reset to page 1 when sorting changes
    router.push(`?${params.toString()}`);
  };

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
          value={sortBy}
          onChange={handleSortChange}
        >
          {options.map((option) => (
            <option key={option.value} value={option.value}>
              {option.label}
            </option>
          ))}
        </select>
      </div>
    </form>
  );
}
