import type { Metadata } from "next";
import { ExceptionDetails } from "@/app/types";
import { auth } from "@/app/lib/auth";
import { checkAccess } from "@/app/lib/checkAccess";
import {
  fetchExceptions,
  fetchExceptionsToday,
} from "@/app/lib/fetchExceptions";
import ExceptionsTable from "@/app/components/exceptionsTable";
import Breadcrumb from "@/app/components/breadcrumb";
import Unauthorised from "@/app/components/unauthorised";
import DataError from "@/app/components/dataError";

export const metadata: Metadata = {
  title: "Exceptions created today - Cohort Manager",
};

export default async function Page(props: {
  params: Promise<{ filter: string }>;
}) {
  const session = await auth();
  const isCohortManager = session?.user
    ? await checkAccess(session.user.uid)
    : false;

  if (!isCohortManager) {
    return <Unauthorised />;
  }

  const breadcrumbItems = [{ label: "Overview", url: "/" }];

  try {
    const params = await props.params;
    const exceptions =
      params.filter === "today"
        ? await fetchExceptionsToday()
        : await fetchExceptions();

    const exceptionDetails: ExceptionDetails[] = exceptions.Items.map(
      (exception: {
        ExceptionId: string;
        DateCreated: Date;
        RuleDescription: string;
        NhsNumber: number;
      }) => ({
        exceptionId: exception.ExceptionId,
        dateCreated: exception.DateCreated,
        shortDescription: exception.RuleDescription,
        nhsNumber: exception.NhsNumber,
      })
    );

    return (
      <>
        <Breadcrumb items={breadcrumbItems} />
        <main className="nhsuk-main-wrapper" id="maincontent" role="main">
          <div className="nhsuk-grid-row">
            <div className="nhsuk-grid-column-full">
              <h1>
                Breast screening exceptions
                <span className="nhsuk-caption-xl">
                  Exceptions created today
                </span>
              </h1>
              <div className="nhsuk-card">
                <div className="nhsuk-card__content">
                  <ExceptionsTable
                    exceptions={exceptionDetails}
                    caption="Breast screening exceptions which have been created today"
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
        <DataError />;
      </>
    );
  }
}
