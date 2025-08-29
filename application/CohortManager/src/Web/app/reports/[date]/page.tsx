import type { Metadata } from "next";
import { auth } from "@/app/lib/auth";
import { canAccessCohortManager } from "@/app/lib/access";
import { fetchReports } from "@/app/lib/fetchReports";
import { formatDate } from "@/app/lib/utils";
import Breadcrumb from "@/app/components/breadcrumb";
import Unauthorised from "@/app/components/unauthorised";
import DataError from "@/app/components/dataError";
import ReportsInformationTable from "@/app/components/reportsInformationTable";
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
  const categoryTitle =
    categoryId === 12
      ? "Possible confusion"
      : categoryId === 13
      ? "NHS number change"
      : String(categoryId);

  try {
    const report = await fetchReports(categoryId, date);

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
              {report ? (
                <div className="nhsuk-card">
                  <div className="nhsuk-card__content">
                    {report.Items?.length ? (
                      <ReportsInformationTable
                        category={categoryId}
                        items={report.Items as readonly ExceptionAPIDetails[]}
                      />
                    ) : (
                      <p>No report available for {formatDate(date)}</p>
                    )}
                  </div>
                </div>
              ) : (
                <p>No report available for {formatDate(date)}</p>
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
