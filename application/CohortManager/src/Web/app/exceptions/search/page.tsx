import type { Metadata } from "next";
import { redirect } from "next/navigation";
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

interface SearchParams {
  nhsNumber?: string;
  page?: string;
}

const BREADCRUMB_ITEMS = [
  { label: "Home", url: "/" },
  { label: "Search exceptions", url: "/exceptions/search" },
];

const PAGE_SIZE = 10;
const RELEVANT_REPORT_CATEGORIES = new Set([12, 13]);

// Helper functions
const parseCurrentPage = (page?: string): number => {
  return Math.max(1, Number.parseInt(page || "1", 10));
};

const transformApiException = (exception: ApiException): ExceptionDetails => {
  const ruleMapping = getRuleMapping(exception.RuleId, exception.RuleDescription);
  return {
    exceptionId: exception.ExceptionId,
    dateCreated: exception.DateCreated,
    shortDescription: ruleMapping.ruleDescription,
    nhsNumber: exception.NhsNumber,
    serviceNowId: exception.ServiceNowId ?? "",
    serviceNowCreatedDate: exception.ServiceNowCreatedDate?.toString()
  };
};

const calculatePaginationInfo = (currentPage: number, itemsLength: number, totalCount: number) => {
  if (totalCount === 0) return { startItem: 0, endItem: 0 };

  const startItem = (currentPage - 1) * PAGE_SIZE + 1;
  const endItem = Math.min(startItem + itemsLength - 1, totalCount);
  return { startItem, endItem };
};

const getCategoryLabel = (category: number): string => {
  return category === 13 ? "NHS Number Change" : "Possible Confusion";
};

const formatReportDate = (dateString: string): string => {
  if (new RegExp(/^\d{4}-\d{2}-\d{2}/).exec(dateString)) {
    return dateString.split('T')[0];
  }

  const date = new Date(dateString);
  return [
    date.getFullYear(),
    String(date.getMonth() + 1).padStart(2, '0'),
    String(date.getDate()).padStart(2, '0')
  ].join('-');
};

const buildReportUrl = (reportDate: string, category: number, nhsNumber: string): string => {
  return `/reports/${formatReportDate(reportDate)}?category=${category}&nhsNumber=${nhsNumber}`;
};

// Component: No NHS Number State
function NoNhsNumberState() {
  return (
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
  );
}

// Component: Error State
function ErrorState({ error }: { readonly error: unknown }) {
  const errorMessage = error instanceof Error
    ? error.message
    : "An error occurred while fetching exceptions. Please try again.";

  return (
    <main className="nhsuk-main-wrapper" id="maincontent" role="main">
      <div className="nhsuk-grid-row">
        <div className="nhsuk-grid-column-full">
          <h1>Search exceptions by NHS number</h1>
          <div className="nhsuk-error-summary">
            <div className="nhsuk-error-summary__body">
              <p>{errorMessage}</p>
            </div>
          </div>
        </div>
      </div>
    </main>
  );
}

// Component: Results Header
function ResultsHeader({ startItem, endItem, totalCount }: {
  readonly startItem: number;
  readonly endItem: number;
  readonly totalCount: number;
}) {
  return (
    <div className="app-form-results-container">
      <h2 className="nhsuk-heading-m nhsuk-u-margin-bottom-0">Exceptions</h2>
      <p className="app-results-text nhsuk-u-font-weight-bold" data-testid="search-exception-count">
        Showing {startItem} to {endItem} of {totalCount} results
      </p>
    </div>
  );
}

// Component: No Exceptions State
function NoExceptionsState({ nhsNumber }: { readonly nhsNumber: string }) {
  return (
    <>
      <h2 className="nhsuk-heading-m nhsuk-u-margin-bottom-5">Exceptions</h2>
      <div className="nhsuk-card nhsuk-u-margin-bottom-5">
        <div className="nhsuk-card__content">
          <p>No exceptions found for NHS Number {nhsNumber}</p>
        </div>
      </div>
    </>
  );
}

// Component: Reports Table Row
function ReportsTableRow({ report, nhsNumber, index }: {
  readonly report: ValidationExceptionReport;
  readonly nhsNumber: string;
  readonly index: number;
}) {
  const categoryLabel = getCategoryLabel(report.Category!);
  const reportUrl = buildReportUrl(report.ReportDate, report.Category!, nhsNumber);
  const formattedDate = new Date(report.ReportDate).toLocaleDateString("en-GB");

  return (
    <tr className="nhsuk-table__row" key={`${report.ReportDate}-${report.Category}-${index}`}>
      <td className="nhsuk-table__cell">{formattedDate}</td>
      <td className="nhsuk-table__cell">{categoryLabel}</td>
      <td className="nhsuk-table__cell">
        <a href={reportUrl} className="nhsuk-link">View report</a>
      </td>
    </tr>
  );
}

// Component: Reports Section
function ReportsSection({ reports, nhsNumber }: {
  readonly reports: ValidationExceptionReport[];
  readonly nhsNumber: string;
}) {
  const filteredReports = reports.filter(report =>
    RELEVANT_REPORT_CATEGORIES.has(report.Category!)
  );

  return (
    <>
      <h2 className="nhsuk-heading-m nhsuk-u-margin-top-5">Reports</h2>
      <div className="nhsuk-card">
        <div className="nhsuk-card__content">
          {filteredReports.length > 0 ? (
            <table className="nhsuk-table" data-testid="reports-table">
              <thead className="nhsuk-table__head">
                <tr className="nhsuk-table__row">
                  <th className="nhsuk-table__header" scope="col">Date</th>
                  <th className="nhsuk-table__header" scope="col">Demographic change</th>
                  <th className="nhsuk-table__header" scope="col">Action</th>
                </tr>
              </thead>
              <tbody className="nhsuk-table__body">
                {filteredReports.map((report, index) => (
                  <ReportsTableRow
                    key={`${report.ReportDate}-${report.Category}-${index}`}
                    report={report}
                    nhsNumber={nhsNumber}
                    index={index}
                  />
                ))}
              </tbody>
            </table>
          ) : (
            <p>No reports available for {nhsNumber}</p>
          )}
        </div>
      </div>
    </>
  );
}

// Main Page Component
export default async function Page({
  searchParams,
}: {
  readonly searchParams?: Promise<SearchParams>;
}) {
  const session = await auth();
  const isCohortManager = await canAccessCohortManager(session);

  if (!isCohortManager) {
    return <Unauthorised />;
  }

  const resolvedSearchParams = searchParams ? await searchParams : {};
  const nhsNumber = resolvedSearchParams.nhsNumber;
  const currentPage = parseCurrentPage(resolvedSearchParams.page);

  if (!nhsNumber) {
    return (
      <>
        <Breadcrumb items={BREADCRUMB_ITEMS} />
        <NoNhsNumberState />
      </>
    );
  }

  const response = await fetchExceptionsByNhsNumber({
    nhsNumber,
    page: currentPage,
    pageSize: PAGE_SIZE,
  });

  const totalCount = response.data.PaginatedExceptions.TotalItems || 0;
  const reportsCount = response.data.Reports?.length || 0;

  if (totalCount === 0 && reportsCount === 0) {
      redirect(`/exceptions/noResults`);
  }

  try {
    const exceptionDetails = response.data.PaginatedExceptions.Items.map(transformApiException);
    const { startItem, endItem } = calculatePaginationInfo(
      currentPage,
      response.data.PaginatedExceptions.Items.length,
      totalCount
    );

    const linkHeader = response.headers?.get("Link") || response.linkHeader;
    const totalPages = response.data.PaginatedExceptions.TotalPages || 1;
    const reports = response.data.Reports;

    return (
      <>
        <Breadcrumb items={BREADCRUMB_ITEMS} />
        <main className="nhsuk-main-wrapper" id="maincontent" role="main">
          <div className="nhsuk-grid-row">
            <div className="nhsuk-grid-column-full">
              <h1 data-testid="heading-search-exceptions">
                Search results for {nhsNumber}
              </h1>

              {exceptionDetails.length > 0 ? (
                <>
                  <ResultsHeader
                    startItem={startItem}
                    endItem={endItem}
                    totalCount={totalCount}
                  />
                  <div className="nhsuk-card nhsuk-u-margin-bottom-5">
                    <div className="nhsuk-card__content">
                      <ExceptionsTable exceptions={exceptionDetails} />
                    </div>
                  </div>
                </>
              ) : (
                <NoExceptionsState nhsNumber={nhsNumber} />
              )}

              {totalPages > 1 && (
                <Pagination
                  linkHeader={linkHeader}
                  currentPage={currentPage}
                  totalPages={totalPages}
                  buildUrl={(page) => `/exceptions/search?nhsNumber=${nhsNumber}&page=${page}`}
                />
              )}

              <ReportsSection reports={reports} nhsNumber={nhsNumber} />
            </div>
          </div>
        </main>
      </>
    );
  } catch (error) {
    return (
      <>
        <Breadcrumb items={BREADCRUMB_ITEMS} />
        <ErrorState error={error} />
      </>
    );
  }
}
