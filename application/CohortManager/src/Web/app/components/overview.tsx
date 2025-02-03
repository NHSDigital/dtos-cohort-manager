import type { Metadata } from "next";
import CardGroup from "@/app/components/cardGroup";
import DataError from "@/app/components/dataError";
import {
  fetchExceptions,
  fetchExceptionsToday,
} from "@/app/lib/fetchExceptions";

export const metadata: Metadata = {
  title: "Overview - Cohort Manager",
};

export default async function Overview() {
  try {
    const exceptions = await fetchExceptions();
    const exceptionsToday = await fetchExceptionsToday();

    const cards = [
      {
        value: exceptions.TotalItems,
        label: "Breast screening exceptions",
        url: "/exceptions-summary",
      },
      {
        value: exceptionsToday.TotalItems,
        label: "Breast screening exceptions created today",
        url: `/exceptions-summary/today`,
      },
    ];

    return (
      <main className="nhsuk-main-wrapper" id="maincontent" role="main">
        <div className="nhsuk-grid-row">
          <div className="nhsuk-grid-column-full">
            <h1>
              Breast screening exceptions
              <span className="nhsuk-caption-xl">Overview</span>
            </h1>
            <CardGroup items={cards} />
          </div>
        </div>
      </main>
    );
  } catch {
    return <DataError />;
  }
}
