import type { Metadata } from "next";
import Breadcrumb from "@/app/components/breadcrumb";

export const metadata: Metadata = {
  title: `Get help with Cohort Manager - ${process.env.SERVICE_NAME} - NHS`,
};

export default async function Page() {
  const breadcrumbItems = [{ label: "Home", url: "/" }];

  return (
    <>
      <Breadcrumb items={breadcrumbItems} />
      <main className="nhsuk-main-wrapper" id="maincontent" role="main">
        <div className="nhsuk-grid-row">
          <div className="nhsuk-grid-column-two-thirds">
            <h1>Get help with Cohort Manager</h1>
            <p>
              Please select from the options below to raise a ticket via your
              service portal:
            </p>
            <ul className="nhsuk-list nhsuk-list--bullet">
              <li>
                <a
                  href="https://nhsdigitallive.service-now.com/csm?id=sc_cat_item&sys_id=ce81c3ae1b1c5190892d4046b04bcb83&sysparm_category=c791adab973b92d0dd80f2df9153afb6&catalog_id=65bcd377c3011200b12d9f2974d3aea0"
                  data-testid="technical-support-link"
                >
                  technical support and general enquiries
                </a>
              </li>
              <li>
                <a
                  href="https://nhsdigitallive.service-now.com/csm?id=sc_cat_item&sys_id=644ffeccc70500104c1bf9f91dc260d8&sysparm_category=c791adab973b92d0dd80f2df9153afb6&catalog_id=65bcd377c3011200b12d9f2974d3aea0"
                  data-testid="report-incident-link"
                >
                  report an incident
                </a>
              </li>
            </ul>
            <p>
              You&apos;ll need to select the service “
              <strong>Digital Screening - Cohort Manager</strong>” when
              submitting the form.
            </p>
          </div>
        </div>
      </main>
    </>
  );
}
