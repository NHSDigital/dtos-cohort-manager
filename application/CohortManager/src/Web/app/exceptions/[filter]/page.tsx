import type { Metadata } from "next";
import { ExceptionDetails } from "@/app/types";
import { auth } from "@/app/lib/auth";
import { canAccessCohortManager } from "@/app/lib/access";
import { fetchExceptions } from "@/app/lib/fetchExceptions";
import { getRuleMapping } from "@/app/lib/ruleMapping";
import ExceptionsTable from "@/app/components/exceptionsTable";
import SortExceptionsForm from "@/app/components/sortExceptionsForm";
import Breadcrumb from "@/app/components/breadcrumb";
import Unauthorised from "@/app/components/unauthorised";
import DataError from "@/app/components/dataError";

export const metadata: Metadata = {
  title: `Raised breast screening exceptions - ${process.env.SERVICE_NAME} - NHS`,
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

  const sortOptions = [
    {
      value: "0",
      label: "Status last updated (most recent first)",
    },
    {
      value: "1",
      label: "Status last updated (oldest first)",
    },
  ];

  try {
    const exceptions = await fetchExceptions({sortOrder: sortBy});

    const exceptionDetails: ExceptionDetails[] = exceptions.data.Items.map(
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
              <h1 data-testid="heading-raised">
                Raised breast screening exceptions
              </h1>

              <div className="app-form-results-container">
                <SortExceptionsForm
                  sortBy={sortBy}
                  options={sortOptions}
                  hiddenText="raised exceptions"
                  testId="sort-raised-exceptions"
                />
                <p
                  className="app-results-text"
                  data-testid="raised-exception-count"
                >
                  Showing {exceptionDetails.length} of {exceptions.data.TotalItems}{" "}
                  results
                </p>
              </div>
              <div className="nhsuk-card nhsuk-u-margin-bottom-5">
                <div className="nhsuk-card__content">
                  <ExceptionsTable
                    exceptions={exceptionDetails}
                    caption="Breast screening exceptions which have been created today"
                  />
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
