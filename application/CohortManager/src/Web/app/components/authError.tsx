import React from "react";
import Link from "next/link";

export default function AuthError() {
  return (
    <>
      <h1>Something went wrong with the CIS2 authentication</h1>
      <p>Please contact us if this error persists.</p>
      <p>
        <Link href="/">Go back to the Homepage</Link>
      </p>
    </>
  );
}
