import CardGroup from "@/app/components/cardGroup";
import DataError from "@/app/components/dataError";
import {
  fetchExceptionsNotRaised,
  fetchExceptionsRaised,
} from "@/app/lib/fetchExceptions";

export default async function Overview() {
  try {
    const exceptions = await fetchExceptionsNotRaised();
    const exceptionsToday = await fetchExceptionsRaised();

    const exceptionItems = [
      {
        value: exceptions.TotalItems,
        label: "Not raised",
        description: "Exceptions to be raised with teams",
        url: "/exceptions",
      },
      {
        value: exceptionsToday.TotalItems,
        label: "Raised",
        description: "Access and amend previously raised exceptions",
        url: `/exceptions/raised`,
      },
    ];

    const reportItems = [
      {
        value: 28, // Showing reports for last 2 weeks
        label: "Reports",
        description: "To manage investigations into demographic changes",
        url: "/reports",
      },
    ];

    return (
      <main className="nhsuk-main-wrapper" id="maincontent" role="main">
        <div className="nhsuk-grid-row">
          <div className="nhsuk-grid-column-full">
            <h1>Breast screening</h1>
            <h2>Exceptions</h2>
            <CardGroup items={exceptionItems} />
            <h2>Reports</h2>
            <CardGroup items={reportItems} />
          </div>
        </div>
      </main>
    );
  } catch {
    return <DataError />;
  }
}
