"use client";
import { useRouter } from "next/navigation";
import { useState, FormEvent, useRef } from "react";

export function SearchNhsNumber() {
  const router = useRouter();
  const [nhsNumber, setNhsNumber] = useState("");
  const errorSpanRef = useRef<HTMLSpanElement>(null);

  const isValidNhsNumber = (value: string): boolean => {
    const cleaned = value.replaceAll(" ", "");
    return cleaned.length === 10 && /^\d+$/.test(cleaned);
  };

  const showError = (message: string) => {
    if (errorSpanRef.current) {
      errorSpanRef.current.textContent = message;
      errorSpanRef.current.classList.add("visible");
    }
  };

  const hideError = () => {
    if (errorSpanRef.current) {
      errorSpanRef.current.classList.remove("visible");
    }
  };

  const hasError = (): boolean => {
    return errorSpanRef.current?.classList.contains("visible") || false;
  };

  const handleSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();

    const cleanedNhsNumber = nhsNumber.replaceAll(" ", "");

    if (!isValidNhsNumber(nhsNumber)) {
      showError("Please enter a valid 10-digit NHS number");
      return;
    }

    hideError();
    router.push(`/exceptions/search?nhsNumber=${cleanedNhsNumber}`);
  };

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const value = e.target.value;
    setNhsNumber(value);

    if (hasError() && value.trim()) {
      hideError();
    }
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
    <form onSubmit={handleSubmit} className="app-search-form">
      <div className="search-input-container">
        <input
          className={`nhsuk-input nhsuk-u-margin-bottom-0 ${hasError() ? "nhsuk-input--error" : ""}`}
          id="nhs-number"
          name="nhsNumber"
          type="text"
          placeholder="Search NHS number"
          value={nhsNumber}
          onChange={handleInputChange}
          onKeyDown={handleKeyDown}
          aria-describedby={hasError() ? "nhs-number-error" : undefined}
        />
        <span
          ref={errorSpanRef}
          className="nhsuk-error-message search-error-message"
          id="nhs-number-error"
        >
          <span className="nhsuk-u-visually-hidden">Error:</span>
        </span>
      </div>
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
