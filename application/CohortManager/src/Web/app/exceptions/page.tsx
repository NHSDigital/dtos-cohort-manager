import type { Metadata } from "next";
import { ExceptionDetails } from "@/app/types";
import { auth } from "@/app/lib/auth";
import { checkAccess } from "@/app/lib/checkAccess";
import { fetchExceptionsNotRaised } from "@/app/lib/fetchExceptions";
import ExceptionsTable from "@/app/components/exceptionsTable";
import Breadcrumb from "@/app/components/breadcrumb";
import Unauthorised from "@/app/components/unauthorised";
import DataError from "@/app/components/dataError";

export const metadata: Metadata = {
  title: `Not raised breast screening exceptions - ${process.env.SERVICE_NAME} - NHS`,
};

export default async function Page() {
  const session = await auth();
  const isCohortManager = session?.user
    ? await checkAccess(session.user.uid)
    : false;

  if (!isCohortManager) {
    return <Unauthorised />;
  }

  const breadcrumbItems = [{ label: "Home", url: "/" }];

  try {
    const exceptions = await fetchExceptionsNotRaised();

    const exceptionDetails: ExceptionDetails[] = exceptions.Items.map(
      (exception: {
        ExceptionId: string;
        DateCreated: Date;
        RuleDescription: string;
        NhsNumber: number;
        ServiceNowId?: string;
        ServiceNowCreatedDate?: Date;
      }) => ({
        exceptionId: exception.ExceptionId,
        dateCreated: exception.DateCreated,
        shortDescription: exception.RuleDescription,
        nhsNumber: exception.NhsNumber,
        serviceNowId: exception.ServiceNowId ?? "",
        serviceNowCreatedDate: exception.ServiceNowCreatedDate,
      })
    );

    return (
      <>
        <Breadcrumb items={breadcrumbItems} />
        <main className="nhsuk-main-wrapper" id="maincontent" role="main">
          <div className="nhsuk-grid-row">
            <div className="nhsuk-grid-column-full">
              <h1>Not raised breast screening exceptions</h1>
              <div className="nhsuk-card">
                <div className="nhsuk-card__content">
                  <ExceptionsTable exceptions={exceptionDetails} />
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
        <DataError />;
      </>
    );
  }
}
