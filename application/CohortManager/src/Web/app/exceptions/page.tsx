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
import Pagination from "@/app/components/pagination";
import UserFeedback from "@/app/components/userFeedback";

export const metadata: Metadata = {
  title: `Not raised breast screening exceptions - ${process.env.SERVICE_NAME} - NHS`,
};

interface ApiException {
  ExceptionId: number;
  NhsNumber: string;
  DateCreated: string;
  RuleId: number;
  RuleDescription: string;
  ServiceNowId: string | null;
  ServiceNowCreatedDate: string | null;
}

export default async function Page({
  searchParams,
}: {
  readonly searchParams?: Promise<{
    readonly sortBy?: string;
    readonly page?: string;
  }>;
}) {
  const session = await auth();
  const isCohortManager = await canAccessCohortManager(session);

  if (!isCohortManager) {
    return <Unauthorised />;
  }

  const breadcrumbItems = [{ label: "Home", url: "/" }];
  const resolvedSearchParams = searchParams ? await searchParams : {};
  const sortBy = resolvedSearchParams.sortBy === "0" ? 0 : 1;
  const currentPage = Math.max(
    1,
    Number.parseInt(resolvedSearchParams.page || "1", 10)
  );

  const sortOptions = [
    {
      value: "0",
      label: "Date exception created (oldest first)",
    },
    {
      value: "1",
      label: "Date exception created (newest first)",
    },
        {
      value: "3",
      label: "NHS Number (Ascending)",
    },
    {
      value: "4",
      label: "NHS Number (Descending)",
    },
  ];

  try {
    const response = await fetchExceptions({
      exceptionStatus: 2,
      sortOrder: sortBy,
      page: currentPage,
    });

    const exceptionDetails: ExceptionDetails[] = response.data.Items.map(
      (exception: ApiException) => {
        const ruleMapping = getRuleMapping(
          exception.RuleId,
          exception.RuleDescription
        );
        return {
          exceptionId: exception.ExceptionId.toString(),
          dateCreated: new Date(exception.DateCreated),
          shortDescription: ruleMapping.ruleDescription,
          nhsNumber: exception.NhsNumber,
          serviceNowId: exception.ServiceNowId ?? "",
          serviceNowCreatedDate: exception.ServiceNowCreatedDate
            ? new Date(exception.ServiceNowCreatedDate)
            : undefined,
        };
      }
    );

    const linkHeader = response.headers?.get("Link") || response.linkHeader;
    const totalPages = response.data.TotalPages || 1;
    const pageSize = 10;
    const totalItems = Number(response.data.TotalItems) || 0;
    const startItem = totalItems > 0 ? (currentPage - 1) * pageSize + 1 : 0;
    const endItem =
      totalItems > 0
        ? Math.min(startItem + response.data.Items.length - 1, totalItems)
        : 0;

    return (
      <>
        <Breadcrumb items={breadcrumbItems} />
        <main className="nhsuk-main-wrapper" id="maincontent" role="main">
          <div className="nhsuk-grid-row">
            <div className="nhsuk-grid-column-full">
              <h1 data-testid="heading-not-raised">
                Not raised breast screening exceptions
              </h1>

              {totalItems === 0 ? (
                <p
                  className="nhsuk-body"
                  data-testid="no-not-raised-exceptions"
                >
                  There are currently no not raised breast screening exceptions.
                </p>
              ) : (
                <>
                  <div className="app-form-results-container">
                    <SortExceptionsForm
                      sortBy={sortBy}
                      options={sortOptions}
                      hiddenText="not raised exceptions"
                      testId="sort-not-raised-exceptions"
                    />
                    <p
                      className="app-results-text"
                      data-testid="not-raised-exception-count"
                    >
                      Showing {startItem} to {endItem} of {totalItems} results
                    </p>
                  </div>

                  <div className="nhsuk-card nhsuk-u-margin-bottom-5">
                    <div className="nhsuk-card__content">
                      <ExceptionsTable exceptions={exceptionDetails} />
                    </div>
                  </div>

                  {totalPages > 1 && (
                    <Pagination
                      linkHeader={linkHeader}
                      currentPage={currentPage}
                      totalPages={totalPages}
                      buildUrl={(page) =>
                        `/exceptions?sortBy=${sortBy}&page=${page}`
                      }
                    />
                  )}
                  <UserFeedback />
                </>
              )}
            </div>
          </div>
        </main>
      </>
    );
  } catch {
    return (
      <>
        <Breadcrumb items={breadcrumbItems} />
        <DataError />
      </>
    );
  }
}
