import Link from "next/link";

export default async function DataError() {
  return (
    <main className="nhsuk-main-wrapper" id="maincontent" role="main">
      <div className="nhsuk-grid-row">
        <div className="nhsuk-grid-column-two-thirds">
          <h1>The exceptions could not be loaded</h1>
          <p>Please try again later.</p>
          <p>
            <Link href="/contact-us">Contact us</Link> if the problem persists
            or you need further assistance.
          </p>
        </div>
      </div>
    </main>
  );
}
