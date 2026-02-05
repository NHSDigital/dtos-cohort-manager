import type { Metadata } from "next";
import { auth } from "@/app/lib/auth";
import { canAccessCohortManager } from "@/app/lib/access";
import { ExceptionStatus } from "@/app/lib/enums/exceptionStatus";
import Breadcrumb from "@/app/components/breadcrumb";
import Unauthorised from "@/app/components/unauthorised";
import ExceptionsPage from "@/app/components/ExceptionsPage";
import FilterExceptionsForm from "@/app/components/filterExceptionsForm";
import { getRuleFilterOptions, validateDateFilter } from "@/app/lib/filterOptions";
import { SortOptions } from "@/app/lib/sortOptions";

export const metadata: Metadata = {
  title: `Raised breast screening exceptions - ${process.env.SERVICE_NAME} - NHS`,
};

export default async function Page({
  searchParams,
}: {
  readonly searchParams?: Promise<{
    readonly sortBy?: string;
    readonly page?: string;
    readonly ruleId?: string;
    readonly dateDay?: string;
    readonly dateMonth?: string;
    readonly dateYear?: string;
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

  // Handle filter parameters
  const ruleId = resolvedSearchParams.ruleId;
  const dateDay = resolvedSearchParams.dateDay;
  const dateMonth = resolvedSearchParams.dateMonth;
  const dateYear = resolvedSearchParams.dateYear;

  // Validate date if provided
  const dateValidation = validateDateFilter(dateDay, dateMonth, dateYear);

  // Build URL with filters
  const buildUrl = (page: number) => {
    const params = new URLSearchParams();
    params.set("sortBy", selectedSortOption);
    params.set("page", page.toString());

    if (ruleId) {
      params.set("ruleId", ruleId);
    }

    if (dateValidation.isValid && dateDay && dateMonth && dateYear) {
      params.set("dateDay", dateDay);
      params.set("dateMonth", dateMonth);
      params.set("dateYear", dateYear);
    }

    return `/exceptions/raised?${params.toString()}`;
  };

  return (
    <>
      <Breadcrumb items={breadcrumbItems} />
      <main className="nhsuk-main-wrapper" id="maincontent" role="main">
        <div className="nhsuk-grid-row">
          <div className="nhsuk-grid-column-full">
            <h1>Raised breast screening exceptions</h1>

            <FilterExceptionsForm
              ruleOptions={getRuleFilterOptions()}
              selectedRuleId={ruleId}
              dateDay={dateDay}
              dateMonth={dateMonth}
              dateYear={dateYear}
              sortBy={selectedSortOption}
              sortOptions={SortOptions}
              page="1"
              dateError={dateValidation.isValid ? undefined : dateValidation.error}
            />

            <ExceptionsPage
              exceptionStatus={ExceptionStatus.Raised}
              title=""
              noResultsMessage="There are currently no raised breast screening exceptions matching the selected filters."
              sortBy={selectedSortOption}
              currentPage={currentPage}
              buildUrl={buildUrl}
              showServiceNowColumn={true}
              tableCaption="Breast screening exceptions which have been created today"
              ruleId={ruleId}
              dateCreated={dateValidation.isValid ? dateValidation.formattedDate : undefined}
            />
          </div>
        </div>
      </main>
    </>
  );
}
