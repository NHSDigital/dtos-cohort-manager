"use client";
import { useRouter } from "next/navigation";
import { useState, FormEvent } from "react";
import {
  SearchType,
  SearchTypeValue,
  SearchTypeLabels,
  SearchTypePlaceholders,
} from "@/app/types/search-types";

export function ExceptionSearch() {
  const router = useRouter();
  const [searchType, setSearchType] = useState<SearchTypeValue>(SearchType.NhsNumber);
  const [searchValue, setSearchValue] = useState("");

  const isValidInput = (value: string, type: SearchTypeValue): boolean => {
    const cleaned = value.trim().replaceAll(" ", "");

    if (type === SearchType.NhsNumber) {
      return cleaned.length === 10 && /^\d+$/.test(cleaned);
    }

    // ExceptionId: must be a valid number
    return /^\d+$/.test(cleaned);
  };

  const handleSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    const cleanedValue = searchValue.trim().replaceAll(" ", "");

    if (!isValidInput(searchValue, searchType)) {
      const searchTypeName = searchType === SearchType.NhsNumber ? "NhsNumber" : "ExceptionId";
      router.push(`/exceptions/noResults?searchType=${searchTypeName}&searchValue=${encodeURIComponent(cleanedValue)}`);
      return;
    }

    const searchTypeName = searchType === SearchType.NhsNumber ? "NhsNumber" : "ExceptionId";
    router.push(`/exceptions/search?searchType=${searchTypeName}&searchValue=${encodeURIComponent(cleanedValue)}`);
  };

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const value = e.target.value;
    setSearchValue(value);
  };

  const handleSearchTypeChange = (e: React.ChangeEvent<HTMLSelectElement>) => {
    setSearchType(Number(e.target.value) as SearchTypeValue);
    setSearchValue("");
  };

  const handleKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === "Enter") {
      e.preventDefault();
      const form = e.currentTarget.form;
      if (form) {
        form.requestSubmit();
      }
    }
  };

  return (
    <form
      className="nhsuk-header__search-form"
      id="search"
      onSubmit={handleSubmit}
    >
      <label className="nhsuk-u-visually-hidden" htmlFor="search-type">
        Search type
      </label>
      <select
        className="nhsuk-select nhsuk-header__search-select"
        id="search-type"
        name="searchType"
        value={searchType}
        onChange={handleSearchTypeChange}
      >
        {Object.entries(SearchTypeLabels).map(([value, label]) => (
          <option key={value} value={value}>
            {label}
          </option>
        ))}
      </select>
      <label className="nhsuk-u-visually-hidden" htmlFor="search-field">
        {SearchTypePlaceholders[searchType]}
      </label>
      <input
        className="nhsuk-header__search-input nhsuk-input"
        id="search-field"
        name="q"
        type="search"
        placeholder={SearchTypePlaceholders[searchType]}
        autoComplete="off"
        value={searchValue}
        onChange={handleInputChange}
        onKeyDown={handleKeyDown}
      />
      <button className="nhsuk-header__search-submit" type="submit">
        <svg
          className="nhsuk-icon nhsuk-icon--search"
          xmlns="http://www.w3.org/2000/svg"
          viewBox="0 0 24 24"
          width="16"
          height="16"
          focusable="false"
          aria-label="Search"
        >
          <title>Search</title>
          <path d="m20.7 19.3-4.1-4.1a7 7 0 1 0-1.4 1.4l4 4.1a1 1 0 0 0 1.5 0c.4-.4.4-1 0-1.4ZM6 11a5 5 0 1 1 10 0 5 5 0 0 1-10 0Z"></path>
        </svg>
      </button>
    </form>
  );
}
