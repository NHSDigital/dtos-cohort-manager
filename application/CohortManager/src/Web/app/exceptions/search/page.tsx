import type { Metadata } from "next";
import { auth } from "@/app/lib/auth";
import { canAccessCohortManager } from "@/app/lib/access";
import { fetchExceptionsByNhsNumber } from "@/app/lib/fetchExceptions";
import { getRuleMapping } from "@/app/lib/ruleMapping";
import ExceptionsTable from "@/app/components/exceptionsTable";
import Breadcrumb from "@/app/components/breadcrumb";
import Unauthorised from "@/app/components/unauthorised";
import { ExceptionDetails } from "@/app/types";
import Pagination from "@/app/components/pagination";

export const metadata: Metadata = {
  title: `Search exceptions by NHS number - ${process.env.SERVICE_NAME} - NHS`,
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

interface ValidationExceptionReport {
  ReportDate: string;
  FileName: string;
  ScreeningName: string;
  CohortName: string;
  Category: number | null;
  ExceptionCount: number;
}

export default async function Page({
  searchParams,
}: {
  readonly searchParams?: Promise<{
    readonly nhsNumber?: string;
    readonly page?: string;
  }>;
}) {
  const session = await auth();
  const isCohortManager = await canAccessCohortManager(session);

  if (!isCohortManager) {
    return <Unauthorised />;
  }

  const breadcrumbItems = [
    { label: "Home", url: "/" },
    { label: "Search exceptions", url: "/exceptions/search" },
  ];

  const resolvedSearchParams = searchParams ? await searchParams : {};
  const nhsNumber = resolvedSearchParams.nhsNumber;
  const currentPage = Math.max(
    1,
    Number.parseInt(resolvedSearchParams.page || "1", 10)
  );

  if (!nhsNumber) {
    return (
      <>
        <Breadcrumb items={breadcrumbItems} />
        <main className="nhsuk-main-wrapper" id="maincontent" role="main">
          <div className="nhsuk-grid-row">
            <div className="nhsuk-grid-column-two-thirds">
              <h1>Search exceptions by NHS number</h1>
              <p className="nhsuk-body">
                Please enter an NHS number in the search box in the header.
              </p>
            </div>
          </div>
        </main>
      </>
    );
  }

  try {
    const response = await fetchExceptionsByNhsNumber({
      nhsNumber,
      page: currentPage,
      pageSize: 10,
    });

    const exceptionDetails: ExceptionDetails[] =
      response.data.Exceptions.Items.map((exception: ApiException) => {
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
      });

    const linkHeader = response.headers?.get("Link") || response.linkHeader;
    const totalPages = response.data.Exceptions.TotalPages || 1;
    const pageSize = 10;
    const totalCount = response.data.Exceptions.TotalCount || 0;
    const reports: ValidationExceptionReport[] = response.data.Reports;
    const startItem = totalCount > 0 ? (currentPage - 1) * pageSize + 1 : 0;
    const endItem =
      totalCount > 0
        ? Math.min(startItem + response.data.Exceptions.Items.length - 1, totalCount)
        : 0;

    return (
      <>
        <Breadcrumb items={breadcrumbItems} />
        <main className="nhsuk-main-wrapper" id="maincontent" role="main">
          <div className="nhsuk-grid-row">
            <div className="nhsuk-grid-column-full">
              <h1 data-testid="heading-search-exceptions">
                Search results for {nhsNumber}
              </h1>

              {totalCount === 0 ? (
                <div className="app-card">
                  <p>No results for {nhsNumber}</p>
                </div>
              ) : (
                <>
                  <div className="app-form-results-container">
                    <h2 className="nhsuk-heading-m nhsuk-u-margin-bottom-0">
                      Exceptions
                    </h2>
                    <p
                      className="app-results-text nhsuk-u-font-weight-bold"
                      data-testid="search-exception-count"
                    >
                      Showing {startItem} to {endItem} of {totalCount} results
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
                      buildUrl={(page) => `/exceptions/search?nhsNumber=${nhsNumber}&page=${page}`}
                    />
                  )}

                  <h2 className="nhsuk-heading-m nhsuk-u-margin-top-5">
                    Reports
                  </h2>

                  <div className="nhsuk-card">
                    <div className="nhsuk-card__content">
                      {(() => {
                        const filteredReports = reports.filter(report => report.Category === 12 || report.Category === 13);

                        return filteredReports.length > 0 ? (
                          <table
                            className="nhsuk-table"
                            data-testid="reports-table"
                          >
                              <thead className="nhsuk-table__head">
                                <tr className="nhsuk-table__row">
                                  <th
                                    className="nhsuk-table__header"
                                    scope="col"
                                  >
                                    Date
                                  </th>
                                  <th
                                    className="nhsuk-table__header"
                                    scope="col"
                                  >
                                    Demographic change
                                  </th>
                                  <th
                                    className="nhsuk-table__header"
                                    scope="col"
                                  >
                                    Action
                                  </th>
                                </tr>
                              </thead>
                              <tbody className="nhsuk-table__body">
                                {filteredReports.map((report, index) => {
                                  const categoryLabel = report.Category === 13 ? "NHS Number Change" : "Possible Confusion";
                                  const reportDate = new Date(report.ReportDate).toISOString().split('T')[0];
                                  const reportUrl = `/reports/${reportDate}?category=${report.Category}&nhsNumber=${nhsNumber}`;

                                  return (
                                    <tr
                                      className="nhsuk-table__row"
                                      key={`${report.ReportDate}-${report.Category}-${index}`}
                                    >
                                      <td className="nhsuk-table__cell">
                                        {new Date(
                                          report.ReportDate
                                        ).toLocaleDateString("en-GB")}
                                      </td>
                                      <td className="nhsuk-table__cell">
                                        {categoryLabel}
                                      </td>
                                      <td className="nhsuk-table__cell">
                                        <a href={reportUrl} className="nhsuk-link">
                                          View report
                                        </a>
                                      </td>
                                    </tr>
                                  );
                                })}
                              </tbody>
                            </table>
                        ) : (
                          <p className="nhsuk-body">
                            No reports for {nhsNumber}.
                          </p>
                        );
                      })()}
                    </div>
                  </div>
                </>
              )}
            </div>
          </div>
        </main>
      </>
    );
  } catch (error) {
    return (
      <>
        <Breadcrumb items={breadcrumbItems} />
        <main className="nhsuk-main-wrapper" id="maincontent" role="main">
          <div className="nhsuk-grid-row">
            <div className="nhsuk-grid-column-full">
              <h1>Search exceptions by NHS number</h1>
              <div className="nhsuk-error-summary">
                <div className="nhsuk-error-summary__body">
                  <p>
                    {error instanceof Error
                      ? error.message
                      : "An error occurred while fetching exceptions. Please try again."}
                  </p>
                </div>
              </div>
            </div>
          </div>
        </main>
      </>
    );
  }
}
