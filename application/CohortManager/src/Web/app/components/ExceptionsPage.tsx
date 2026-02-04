import { ExceptionDetails } from "@/app/types";
import { ExceptionStatus } from "@/app/lib/enums/exceptionStatus";
import { fetchExceptions } from "@/app/lib/fetchExceptions";
import { getRuleMapping } from "@/app/lib/ruleMapping";
import { getSortOption } from "@/app/lib/sortOptions";
import ExceptionsTable from "@/app/components/exceptionsTable";
import DataError from "@/app/components/dataError";
import Pagination from "@/app/components/pagination";
import UserFeedback from "@/app/components/userFeedback";

interface ApiException {
  ExceptionId: number;
  NhsNumber: string;
  DateCreated: string;
  RuleId: number;
  RuleDescription: string;
  ServiceNowId: string | null;
  ServiceNowCreatedDate: string | null;
}

interface ExceptionsPageProps {
  exceptionStatus: ExceptionStatus;
  title: string;
  noResultsMessage: string;
  sortBy: string;
  currentPage: number;
  buildUrl: (page: number) => string;
  showServiceNowColumn?: boolean;
  tableCaption?: string;
  ruleId?: string;
  dateCreated?: string;
}

export default async function ExceptionsPage({
  exceptionStatus,
  title,
  noResultsMessage,
  sortBy,
  currentPage,
  buildUrl,
  showServiceNowColumn = false,
  tableCaption,
  ruleId,
  dateCreated,
}: Readonly<ExceptionsPageProps>) {
  const sortOption = getSortOption(sortBy);

  try {
    const response = await fetchExceptions({
      exceptionStatus,
      sortOrder: sortOption.sortOrder,
      sortBy: sortOption.sortBy,
      page: currentPage,
      ruleIds: ruleId ? [ruleId] : undefined,
      date: dateCreated,
    });

    const exceptionDetails: ExceptionDetails[] = response.data.Items.map(
      (exception: ApiException) => {
        const ruleMapping = getRuleMapping(
          exception.RuleId,
          exception.RuleDescription
        );
        return {
          exceptionId: exception.ExceptionId.toString(),
          dateCreated: new Date(exception.DateCreated),
          shortDescription: ruleMapping.ruleDescription,
          nhsNumber: exception.NhsNumber,
          serviceNowId: exception.ServiceNowId ?? "",
          serviceNowCreatedDate: exception.ServiceNowCreatedDate
            ? new Date(exception.ServiceNowCreatedDate)
            : undefined,
        };
      }
    );

    const linkHeader = response.headers?.get("Link") || response.linkHeader;
    const totalPages = response.data.TotalPages || 1;
    const pageSize = 10;
    const totalItems = Number(response.data.TotalItems) || 0;
    const startItem = totalItems > 0 ? (currentPage - 1) * pageSize + 1 : 0;
    const endItem =
      totalItems > 0
        ? Math.min(startItem + response.data.Items.length - 1, totalItems)
        : 0;

    return (
      <div className="nhsuk-grid-column-full">
        {title && <h1>{title}</h1>}

        {totalItems === 0 ? (
          <p className="nhsuk-body">
            {noResultsMessage}
          </p>
        ) : (
          <>
            <p
              className="app-results-text"
              data-testid={exceptionStatus === ExceptionStatus.Raised ? "raised-exception-count" : "not-raised-exception-count"}
            >
              Showing {startItem} to {endItem} of {totalItems} results
            </p>

            <div className="nhsuk-card nhsuk-u-margin-bottom-5">
              <div className="nhsuk-card__content">
                <ExceptionsTable
                  exceptions={exceptionDetails}
                  caption={tableCaption}
                  showServiceNowColumn={showServiceNowColumn}
                />
              </div>
            </div>

            {totalPages > 1 && (
              <Pagination
                linkHeader={linkHeader}
                currentPage={currentPage}
                totalPages={totalPages}
                buildUrl={buildUrl}
              />
            )}
            <UserFeedback />
          </>
        )}
      </div>
    );
  } catch {
    return <DataError />;
  }
}
