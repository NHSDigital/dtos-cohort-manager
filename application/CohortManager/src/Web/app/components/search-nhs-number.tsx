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
    <form onSubmit={handleSubmit} className="nhsuk-u-margin-bottom-0">
      <input
        className="nhsuk-input nhsuk-u-margin-bottom-0"
        id="nhs-number"
        name="nhsNumber"
        type="text"
        placeholder="Search NHS number"
        value={nhsNumber}
        onChange={(e) => setNhsNumber(e.target.value)}
        onKeyDown={handleKeyDown}
        style={{ height: "8px", padding: "8px" }}
      />
    </form>
  );
}
