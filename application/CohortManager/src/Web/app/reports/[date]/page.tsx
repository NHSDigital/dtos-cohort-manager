import type { Metadata } from "next";
import { auth } from "@/app/lib/auth";
import { canAccessCohortManager } from "@/app/lib/access";
import { fetchReports } from "@/app/lib/fetchReports";
import { formatDate } from "@/app/lib/utils";
import Breadcrumb from "@/app/components/breadcrumb";
import Unauthorised from "@/app/components/unauthorised";
import DataError from "@/app/components/dataError";
import ReportsInformationTable from "@/app/components/reportsInformationTable";
import Pagination from "@/app/components/pagination";
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
  const currentPage = Math.max(
    1,
    Number.parseInt(resolvedSearchParams.page || "1", 10)
  );
  const pageSize = 20;

  let categoryTitle = String(categoryId);
  if (categoryId === 12) {
    categoryTitle = "Possible confusion";
  } else if (categoryId === 13) {
    categoryTitle = "NHS number change";
  }

  try {
    const response = await fetchReports(categoryId, date, pageSize, currentPage);
    const reportData = response.data;
    const linkHeader = response.headers?.get("Link") || response.linkHeader;

    const totalPages = reportData.TotalPages || 1;
    const totalItems = Number(reportData.TotalItems) || 0;
    const startItem = totalItems > 0 ? (currentPage - 1) * pageSize + 1 : 0;
    const endItem =
      totalItems > 0
        ? Math.min(startItem + reportData.Items.length - 1, totalItems)
        : 0;

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
                      {reportData.Items?.length ? (
                        <ReportsInformationTable
                          category={categoryId}
                          items={
                            reportData.Items as readonly ExceptionAPIDetails[]
                          }
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
