import type { Metadata } from "next";
import { ExceptionDetails } from "@/app/types";
import { auth } from "@/app/lib/auth";
import { canAccessCohortManager } from "@/app/lib/access";
import { fetchExceptionsNotRaisedSorted } from "@/app/lib/fetchExceptions";
import { getRuleMapping } from "@/app/lib/ruleMapping";
import ExceptionsTable from "@/app/components/exceptionsTable";
import SortExceptionsForm from "@/app/components/sortExceptionsForm";
import Breadcrumb from "@/app/components/breadcrumb";
import Unauthorised from "@/app/components/unauthorised";
import DataError from "@/app/components/dataError";

export const metadata: Metadata = {
  title: `Not raised breast screening exceptions - ${process.env.SERVICE_NAME} - NHS`,
};

export default async function Page({
  searchParams,
}: {
  readonly searchParams?: Promise<{ readonly sortOrder?: string }>;
}) {
  const session = await auth();
  const isCohortManager = await canAccessCohortManager(session);

  if (!isCohortManager) {
    return <Unauthorised />;
  }

  const breadcrumbItems = [{ label: "Home", url: "/" }];
  const resolvedSearchParams = searchParams ? await searchParams : {};
  const sortOrder = resolvedSearchParams.sortOrder === "1" ? 1 : 0;

  const sortOptions = [
    {
      value: "0",
      label: "Date exception created (newest first)",
    },
    {
      value: "1",
      label: "Date exception created (oldest first)",
    },
  ];

  try {
    const exceptions = await fetchExceptionsNotRaisedSorted(sortOrder);

    const exceptionDetails: ExceptionDetails[] = exceptions.Items.map(
      (exception: {
        ExceptionId: string;
        DateCreated: Date;
        RuleDescription: string;
        RuleId: number;
        NhsNumber: number;
        ServiceNowId?: string;
        ServiceNowCreatedDate?: Date;
      }) => {
        const ruleMapping = getRuleMapping(
          exception.RuleId,
          exception.RuleDescription
        );
        return {
          exceptionId: exception.ExceptionId,
          dateCreated: exception.DateCreated,
          shortDescription: ruleMapping.ruleDescription,
          nhsNumber: exception.NhsNumber,
          serviceNowId: exception.ServiceNowId ?? "",
          serviceNowCreatedDate: exception.ServiceNowCreatedDate,
        };
      }
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
                <SortExceptionsForm
                  sortOrder={sortOrder}
                  options={sortOptions}
                  hiddenText="not raised exceptions"
                  testId="sort-not-raised-exceptions"
                />
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
