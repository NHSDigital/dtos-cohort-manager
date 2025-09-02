import type { Metadata } from "next";
import { auth } from "@/app/lib/auth";
import { canAccessCohortManager } from "@/app/lib/access";
import Breadcrumb from "@/app/components/breadcrumb";
import DataError from "@/app/components/dataError";
import ReportsTable from "@/app/components/reportsTable";
import Unauthorised from "@/app/components/unauthorised";
import { type ReportDetails } from "@/app/types";
import { formatDate, formatIsoDate } from "../lib/utils";

export const metadata: Metadata = {
  title: `Reports - ${process.env.SERVICE_NAME} - NHS`,
};

export default async function Page() {
  const session = await auth();
  const isCohortManager = await canAccessCohortManager(session);

  if (!isCohortManager) {
    return <Unauthorised />;
  }

  const breadcrumbItems = [{ label: "Home", url: "/" }];

  try {
    // Build a 2-week date range ending today and generate 2 reports per day
    const today = new Date();
    const start = new Date(today);
    start.setDate(today.getDate() - 13);

    const reports: ReportDetails[] = [];

    // Iterate from today backwards to start for descending order
    for (let i = 0; i < 14; i++) {
      const d = new Date(today);
      d.setDate(today.getDate() - i);
      const dateString = formatIsoDate(d);

      reports.push({
        reportId: `${dateString}?category=13`,
        dateCreated: dateString,
        category: 13,
      });

      reports.push({
        reportId: `${dateString}?category=12`,
        dateCreated: dateString,
        category: 12,
      });
    }

    return (
      <>
        <Breadcrumb items={breadcrumbItems} />
        <main className="nhsuk-main-wrapper" id="maincontent" role="main">
          <div className="nhsuk-grid-row">
            <div className="nhsuk-grid-column-full">
              <h1 data-testid="heading-report">Reports</h1>
              <p
                className="app-results-text nhsuk-u-margin-bottom-4"
                data-testid="reports-date-range"
              >
                Showing reports for {formatDate(formatIsoDate(start))} to{" "}
                {formatDate(formatIsoDate(today))}
              </p>
              <div className="nhsuk-card">
                <div className="nhsuk-card__content">
                  <ReportsTable
                    reports={reports}
                    caption={`All reports for last 2 weeks ${formatDate(
                      formatIsoDate(start)
                    )} to ${formatDate(formatIsoDate(today))}`}
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
        <DataError entity="reports" />
      </>
    );
  }
}
