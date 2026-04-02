import type { Metadata } from "next";
import { auth } from "@/app/lib/auth";
import { canAccessCohortManager } from "@/app/lib/access";
import { fetchExceptions } from "@/app/lib/fetchExceptions";
import { formatDate } from "@/app/lib/utils";
import Breadcrumb from "@/app/components/breadcrumb";
import Unauthorised from "@/app/components/unauthorised";
import DataError from "@/app/components/dataError";
import ReportsInformationTable from "@/app/components/reportsInformationTable";
import Pagination from "@/app/components/pagination";
import UserFeedback from "@/app/components/userFeedback";
import { type ExceptionAPIDetails } from "@/app/types/exceptionsApi";

export const metadata: Metadata = {
  title: `View report - ${process.env.SERVICE_NAME} - NHS`,
};

export default async function Page(props: {
  readonly params: Promise<{
    readonly date: string;
  }>;
  readonly searchParams?: Promise<{
    readonly category?: string;
    readonly page?: string;
    readonly nhsNumber?: string;
  }>;
}) {
  const session = await auth();
  const isCohortManager = await canAccessCohortManager(session);

  if (!isCohortManager) {
    return <Unauthorised />;
  }

  const breadcrumbItems = [
    { label: "Home", url: "/" },
    { label: "Reports", url: "/reports" },
  ];

  const params = await props.params;
  const date = params.date;
  const resolvedSearchParams = props.searchParams
    ? await props.searchParams
    : {};
  const categoryId = Number(resolvedSearchParams.category);
  const nhsNumber = resolvedSearchParams.nhsNumber;
  const currentPage = Math.max(
    1,
    Number.parseInt(resolvedSearchParams.page || "1", 10)
  );
  const pageSize = 20;

  const categoryTitles: Record<number, string> = {
    12: "Possible Confusion",
    13: "NHS Number Change",
  };
  const categoryTitle = categoryTitles[categoryId] ?? String(categoryId);

  try {
    const response = await fetchExceptions({
      exceptionStatus: 1,
      sortOrder: 1,
      exceptionCategory: categoryId,
      isReport: true,
      reportDate: date,
      pageSize,
      page: currentPage,
    });
    const reportData = response.data;
    const linkHeader = response.headers?.get("Link") || response.linkHeader;

    // Filter items by NHS number if provided
    const filteredItems = nhsNumber
      ? reportData.Items.filter((item: ExceptionAPIDetails) => item.NhsNumber === nhsNumber)
      : reportData.Items;

    const totalPages = reportData.TotalPages || 1;
    const totalItems = nhsNumber ? filteredItems.length : Number(reportData.TotalItems) || 0;
    const filteredStartValue = totalItems > 0 ? 1 : 0;
    const paginatedStartValue = totalItems > 0 ? (currentPage - 1) * pageSize + 1 : 0;
    const startItem = nhsNumber
      ? Math.max(1, filteredStartValue)
      : Math.max(0, paginatedStartValue);
    const endItem = Math.max(0, totalItems);

    return (
      <>
        <Breadcrumb items={breadcrumbItems} />
        <main className="nhsuk-main-wrapper" id="maincontent" role="main">
          <div className="nhsuk-grid-row">
            <div className="nhsuk-grid-column-full">
              <h1 data-testid="heading-report-details">
                {categoryTitle}
                <span className="nhsuk-caption-xl">{formatDate(date)}</span>
              </h1>
              {totalItems === 0 ? (
                <p>No report available for {formatDate(date)}</p>
              ) : (
                <>
                  <p
                    className="app-results-text nhsuk-u-margin-bottom-4"
                    data-testid="report-count"
                  >
                    Showing {startItem} to {endItem} of {totalItems} results
                  </p>

                  <div className="nhsuk-card nhsuk-u-margin-bottom-5">
                    <div className="nhsuk-card__content">
                      {filteredItems?.length ? (
                        <ReportsInformationTable
                          category={categoryId}
                          items={filteredItems as readonly ExceptionAPIDetails[]}
                        />
                      ) : (
                        <p>No report available for {formatDate(date)}</p>
                      )}
                    </div>
                  </div>

                  {totalPages > 1 && (
                    <Pagination
                      linkHeader={linkHeader}
                      currentPage={currentPage}
                      totalPages={totalPages}
                      buildUrl={(page) =>
                        `/reports/${date}?category=${categoryId}&page=${page}`
                      }
                    />
                  )}
                </>
              )}
            </div>
          </div>
          <UserFeedback />
        </main>
      </>
    );
  } catch {
    return (
      <>
        <Breadcrumb items={breadcrumbItems} />
        <DataError entity="reports" />
      </>
    );
  }
}
