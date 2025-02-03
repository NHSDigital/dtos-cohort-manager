export default async function DataError() {
  return (
    <main className="nhsuk-main-wrapper" id="maincontent" role="main">
      <div className="nhsuk-grid-row">
        <div className="nhsuk-grid-column-two-thirds">
          <h1>The exceptions could not be loaded</h1>
          <p>
            There was an error loading the exceptions. Please try again later.
          </p>
          <p>
            <a href="mailto:england.digitalscreening@nhs.net">Contact us</a> if
            the problem persists or you need further assistance.
          </p>
        </div>
      </div>
    </main>
  );
}
