import type { Metadata } from "next";
import { auth } from "@/app/lib/auth";
import { canAccessCohortManager } from "@/app/lib/access";
import { ExceptionStatus } from "@/app/lib/enums/exceptionStatus";
import Breadcrumb from "@/app/components/breadcrumb";
import Unauthorised from "@/app/components/unauthorised";
import ExceptionsPage from "@/app/components/ExceptionsPage";

export const metadata: Metadata = {
  title: `Not raised breast screening exceptions - ${process.env.SERVICE_NAME} - NHS`,
};

export default async function Page({
  searchParams,
}: {
  readonly searchParams?: Promise<{
    readonly sortBy?: string;
    readonly page?: string;
  }>;
}) {
  const session = await auth();
  const isCohortManager = await canAccessCohortManager(session);

  if (!isCohortManager) {
    return <Unauthorised />;
  }

  const breadcrumbItems = [{ label: "Home", url: "/" }];
  const resolvedSearchParams = searchParams ? await searchParams : {};
  const selectedSortOption = resolvedSearchParams.sortBy || "1";
  const currentPage = Math.max(
    1,
    Number.parseInt(resolvedSearchParams.page || "1", 10)
  );

  return (
    <>
      <Breadcrumb items={breadcrumbItems} />
      <main className="nhsuk-main-wrapper" id="maincontent" role="main">
        <div className="nhsuk-grid-row">
          <ExceptionsPage
            exceptionStatus={ExceptionStatus.NotRaised}
            title="Not raised breast screening exceptions"
            noResultsMessage="There are currently no not raised breast screening exceptions."
            sortBy={selectedSortOption}
            currentPage={currentPage}
            buildUrl={(page) => `/exceptions?sortBy=${selectedSortOption}&page=${page}`}
          />
        </div>
      </main>
    </>
  );
}
