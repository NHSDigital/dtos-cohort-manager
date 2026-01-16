import type { Metadata } from "next";
import { auth } from "@/app/lib/auth";
import { canAccessCohortManager } from "@/app/lib/access";
import Breadcrumb from "@/app/components/breadcrumb";
import Unauthorised from "@/app/components/unauthorised";

export const metadata: Metadata = {
  title: `No results - ${process.env.SERVICE_NAME} - NHS`,
};

const BREADCRUMB_ITEMS = [
  { label: "Home", url: "/" },
  { label: "Search exceptions", url: "/exceptions/search" },
];

interface SearchParams {
  searchParams: {
    searchType?: "NhsNumber" | "ExceptionId";
    searchValue?: string;
  };
}

export default async function NoResultsPage({ searchParams }: Readonly<SearchParams>) {
  const session = await auth();
  const isCohortManager = await canAccessCohortManager(session);

  if (!isCohortManager) {
    return <Unauthorised />;
  }

  const { searchType, searchValue } = searchParams;
  const isExceptionIdSearch = searchType === "ExceptionId";

  return (
    <>
      <Breadcrumb items={BREADCRUMB_ITEMS} />
      <main className="nhsuk-main-wrapper" id="maincontent" role="main">
        <div className="nhsuk-grid-row">
          <div className="nhsuk-grid-column-two-thirds">
            {isExceptionIdSearch ? (
              <>
                <h1>No results for Exception ID {searchValue}</h1>
                <div className="nhsuk-u-margin-top-4">
                </div>
              </>
            ) : (
              <>
                <h1>No results</h1>
                <div className="nhsuk-u-margin-top-4">
                  <p>Try checking the NHS number:</p>
                  <ul className="nhsuk-list nhsuk-list--bullet">
                    <li>is 10 digits (like 999 123 4567)</li>
                    <li>does not contain special characters</li>
                    <li>does not contain letters</li>
                  </ul>
                </div>
              </>
            )}
          </div>
        </div>
      </main>
    </>
  );
}
