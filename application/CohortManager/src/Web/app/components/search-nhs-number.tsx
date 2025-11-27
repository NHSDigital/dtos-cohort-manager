"use client";
import { useRouter } from "next/navigation";
import { useState, FormEvent } from "react";

export function SearchNhsNumber() {
  const router = useRouter();
  const [nhsNumber, setNhsNumber] = useState("");

  const handleSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    const cleanedNhsNumber = nhsNumber.replaceAll(" ", "");
    if (cleanedNhsNumber.length === 10 && /^\d+$/.test(cleanedNhsNumber)) {
      router.push(`/exceptions/search?nhsNumber=${cleanedNhsNumber}`);
    } else {
      alert("Please enter a valid 10-digit NHS number");
    }
  };

  const handleKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === "Enter") {
      e.preventDefault();
      handleSubmit(e as unknown as FormEvent<HTMLFormElement>);
    }
  };

  return (
    <form onSubmit={handleSubmit} className="app-search-form">
      <input
        className="nhsuk-input nhsuk-u-margin-bottom-0"
        id="nhs-number"
        name="nhsNumber"
        type="text"
        placeholder="Search NHS number"
        value={nhsNumber}
        onChange={(e) => setNhsNumber(e.target.value)}
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
