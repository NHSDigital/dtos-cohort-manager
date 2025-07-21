import type { Metadata } from "next";
import Breadcrumb from "@/app/components/breadcrumb";
import Link from "next/link";

export const metadata: Metadata = {
  title: `Terms and conditions - ${process.env.SERVICE_NAME} - NHS`,
};

export default async function Page() {
  const breadcrumbItems = [{ label: "Home", url: "/" }];

  return (
    <>
      <Breadcrumb items={breadcrumbItems} />
      <main className="nhsuk-main-wrapper" id="maincontent" role="main">
        <div className="nhsuk-grid-row">
          <div className="nhsuk-grid-column-two-thirds">
            <h1>Terms and conditions</h1>
            <p>
              The content, data and services on cohort manager are delivered by
              NHS England.
            </p>
            <p>
              When you log in to Cohort Manager&apos;s exception visualisation
              user interface (UI), through the{" "}
              <a
                href="https://digital.nhs.uk/services/care-identity-service"
                data-testid="CIS-link"
              >
                Care Identity Service (CIS)
              </a>
              , you are accepting the{" "}
              <a
                href="https://digital.nhs.uk/services/care-identity-service/registration-authority-users/registration-authority-help/privacy-notice#terms-and-conditions"
                data-testid="cis-and-nhs-terms-link"
              >
                CIS and NHS Spine terms and conditions
              </a>{" "}
              and the{" "}
              <Link href="/cookies-policy" data-testid="cookies-policy-link">
                cookies policy
              </Link>
              .
            </p>
            <p>
              Your access to participant and exception data will be used solely
              to support the use case of raising exceptions to National Back
              Office (NBO). Data viewed in the UI will not be further forwarded
              to users or systems outside of the cohort manager platform, and
              NBO.
            </p>
            <p>
              Data displayed in the UI is only retained for the duration of an
              active exception handling case. Once the exception is resolved,
              there is no need to retain it in cohort manager.
            </p>
            <p>
              You acknowledge that your access may also be audited to ensure you
              adhere to these conditions.
            </p>
          </div>
        </div>
      </main>
    </>
  );
}
