import type { Metadata } from "next";
import Link from "next/link";

export const metadata: Metadata = {
  title: `We cannot find the page you’re looking for - ${process.env.SERVICE_NAME} - NHS`,
};

export default function Page() {
  return (
    <main className="nhsuk-main-wrapper" id="maincontent" role="main">
      <div className="nhsuk-grid-row">
        <div className="nhsuk-grid-column-two-thirds">
          <h1>We cannot find the page you’re looking for</h1>
          <p>
            <Link href="/">Return to the homepage</Link>
          </p>
        </div>
      </div>
    </main>
  );
}
