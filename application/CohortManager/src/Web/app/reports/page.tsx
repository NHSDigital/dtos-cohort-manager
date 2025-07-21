import type { Metadata } from "next";
import Breadcrumb from "@/app/components/breadcrumb";
import ReportsTable from "@/app/components/reportsTable";

export const metadata: Metadata = {
  title: `Reports - ${process.env.SERVICE_NAME} - NHS`,
};

export default async function Page() {
  const breadcrumbItems = [{ label: "Home", url: "/" }];

  return (
    <>
      <Breadcrumb items={breadcrumbItems} />
      <main className="nhsuk-main-wrapper" id="maincontent" role="main">
        <div className="nhsuk-grid-row">
          <div className="nhsuk-grid-column-two-thirds">
            <h1 data-testid="heading-report">Reports</h1>
            <ReportsTable reports={[]} />
          </div>
        </div>
      </main>
    </>
  );
}
