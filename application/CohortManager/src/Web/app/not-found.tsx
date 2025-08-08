import type { Metadata } from "next";
import Link from "next/link";

export const metadata: Metadata = {
  title: `Page not found - ${process.env.SERVICE_NAME} - NHS`,
};

export default function Page() {
  return (
    <main className="nhsuk-main-wrapper" id="maincontent" role="main">
      <div className="nhsuk-grid-row">
        <div className="nhsuk-grid-column-two-thirds">
          <h1>Page not found</h1>
          <p>
            <Link href="/">Return to the homepage</Link>
          </p>
          <p>
            <Link href="/contact-us">Contact us</Link> if the problem persists
            or you need further assistance.
          </p>
        </div>
      </div>
    </main>
  );
}
