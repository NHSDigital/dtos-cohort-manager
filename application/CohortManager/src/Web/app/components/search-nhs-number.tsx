"use client";
import { useRouter } from "next/navigation";
import { useState, FormEvent } from "react";

export function SearchNhsNumber() {
  const router = useRouter();
  const [nhsNumber, setNhsNumber] = useState("");

  const isValidNhsNumber = (value: string): boolean => {
    const cleaned = value.replaceAll(" ", "");
    return cleaned.length === 10 && /^\d+$/.test(cleaned);
  };

  const handleSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    const cleanedNhsNumber = nhsNumber.replaceAll(" ", "");

    if (!isValidNhsNumber(nhsNumber)) {
      router.push(`/exceptions/noResults`);
      return;
    }

    router.push(`/exceptions/search?nhsNumber=${encodeURIComponent(cleanedNhsNumber)}`);
  };

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const value = e.target.value;
    setNhsNumber(value);
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
        <label className="nhsuk-u-visually-hidden" htmlFor="search-field">
          Search by NHS number
        </label>
        <input
          className="nhsuk-header__search-input nhsuk-input"
          id="search-field"
          name="q"
          type="search"
          placeholder="Search by NHS Number"
          autoComplete="off"
          value={nhsNumber}
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
