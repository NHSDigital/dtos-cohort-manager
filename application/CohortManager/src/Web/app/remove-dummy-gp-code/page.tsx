import type { Metadata } from "next";
import Breadcrumb from "@/app/components/breadcrumb";
import RemoveDummyGpCodeForm from "@/app/components/removeDummyGpCodeForm";

export const metadata: Metadata = {
  title: `Remove Dummy GP Code - ${process.env.SERVICE_NAME} - NHS`,
};

export default async function Page(props: {
  readonly searchParams?: Promise<{
    readonly success?: string;
  }>;
}) {
  const resolvedSearchParams = props.searchParams
    ? await props.searchParams
    : {};
  const isSuccess = resolvedSearchParams.success === "true";

  const breadcrumbItems = [{ label: "Home", url: "/" }];

  return (
    <>
      <Breadcrumb items={breadcrumbItems} />
      <main className="nhsuk-main-wrapper" id="maincontent" role="main">
        <div className="nhsuk-grid-row">
          <div className="nhsuk-grid-column-two-thirds">
            {isSuccess ? (
              <div className="nhsuk-panel nhsuk-panel--confirmation nhsuk-u-margin-bottom-5" data-testid="success-panel">
                <h1 className="nhsuk-panel__title">Request submitted successfully</h1>
                <div className="nhsuk-panel__body">
                  Your request to remove the dummy GP Code has been accepted.
                </div>
              </div>
            ) : (
              <RemoveDummyGpCodeForm />
            )}
          </div>
        </div>
      </main>
    </>
  );
}
