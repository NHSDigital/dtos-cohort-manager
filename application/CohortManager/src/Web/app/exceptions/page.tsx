import type { Metadata } from "next";
import { ExceptionDetails } from "@/app/types";
import { auth } from "@/app/lib/auth";
import { canAccessCohortManager } from "@/app/lib/access";
import { fetchExceptionsNotRaisedSorted } from "@/app/lib/fetchExceptions";
import ExceptionsTable from "@/app/components/exceptionsTable";
import Breadcrumb from "@/app/components/breadcrumb";
import Unauthorised from "@/app/components/unauthorised";
import DataError from "@/app/components/dataError";

export const metadata: Metadata = {
  title: `Not raised breast screening exceptions - ${process.env.SERVICE_NAME} - NHS`,
};

export default async function Page({
  searchParams,
}: {
  readonly searchParams?: Promise<{ readonly sortBy?: string }>;
}) {
  const session = await auth();
  const isCohortManager = await canAccessCohortManager(session);

  if (!isCohortManager) {
    return <Unauthorised />;
  }

  const breadcrumbItems = [{ label: "Home", url: "/" }];
  const resolvedSearchParams = searchParams ? await searchParams : {};
  const sortBy = resolvedSearchParams.sortBy === "1" ? 1 : 0;

  try {
    const exceptions = await fetchExceptionsNotRaisedSorted(sortBy);

    const exceptionDetails: ExceptionDetails[] = exceptions.Items.map(
      (exception: {
        ExceptionId: string;
        DateCreated: Date;
        RuleDescription: string;
        NhsNumber: number;
        ServiceNowId?: string;
        ServiceNowCreatedDate?: Date;
      }) => ({
        exceptionId: exception.ExceptionId,
        dateCreated: exception.DateCreated,
        shortDescription: exception.RuleDescription,
        nhsNumber: exception.NhsNumber,
        serviceNowId: exception.ServiceNowId ?? "",
        serviceNowCreatedDate: exception.ServiceNowCreatedDate,
      })
    );

    return (
      <>
        <Breadcrumb items={breadcrumbItems} />
        <main className="nhsuk-main-wrapper" id="maincontent" role="main">
          <div className="nhsuk-grid-row">
            <div className="nhsuk-grid-column-full">
              <h1 data-testid="heading-not-raised">
                Not raised breast screening exceptions
              </h1>

              <div className="app-form-results-container">
                <form method="GET">
                  <div className="nhsuk-form-group app-form-group--inline">
                    <label className="nhsuk-label" htmlFor="sort-exceptions">
                      Sort{" "}
                      <span className="nhsuk-u-visually-hidden">
                        not raised exceptions{" "}
                      </span>{" "}
                      by
                    </label>
                    <div className="form-inline-row">
                      <select
                        className="nhsuk-select"
                        id="sort-exceptions"
                        name="sortBy"
                        defaultValue={String(sortBy)}
                      >
                        <option value="0">
                          Date exception created (newest first)
                        </option>
                        <option value="1">
                          Date exception created (oldest first)
                        </option>
                      </select>
                      <button
                        className="nhsuk-button app-button--small"
                        data-module="nhsuk-button"
                        type="submit"
                      >
                        Apply
                      </button>
                    </div>
                  </div>
                </form>
                <p
                  className="app-results-text"
                  data-testid="not-raised-exception-count"
                >
                  Showing {exceptionDetails.length} of {exceptions.TotalItems}{" "}
                  results
                </p>
              </div>
              <div className="nhsuk-card">
                <div className="nhsuk-card__content">
                  <ExceptionsTable exceptions={exceptionDetails} />
                </div>
              </div>
            </div>
          </div>
        </main>
      </>
    );
  } catch {
    return (
      <>
        <Breadcrumb items={breadcrumbItems} />
        <DataError />;
      </>
    );
  }
}
