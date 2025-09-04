import type { Metadata } from "next";
import { ExceptionDetails } from "@/app/types";
import { auth } from "@/app/lib/auth";
import { canAccessCohortManager } from "@/app/lib/access";
import { fetchExceptions } from "@/app/lib/fetchExceptions";
import { getRuleMapping } from "@/app/lib/ruleMapping";
import ExceptionsTable from "@/app/components/exceptionsTable";
import SortExceptionsForm from "@/app/components/sortExceptionsForm";
import Breadcrumb from "@/app/components/breadcrumb";
import Unauthorised from "@/app/components/unauthorised";
import DataError from "@/app/components/dataError";
import Pagination from "@/app/components/pagination";

export const metadata: Metadata = {
  title: `Not raised breast screening exceptions - ${process.env.SERVICE_NAME} - NHS`,
};

interface ApiException {
  ExceptionId: number;
  NhsNumber: string;
  DateCreated: string;
  RuleId: number;
  RuleDescription: string;
  ServiceNowId: string | null;
  ServiceNowCreatedDate: string | null;
}

interface PaginationLinks {
  first?: string;
  previous?: string;
  next?: string;
  last?: string;
}

interface LinkBasedPagination {
  links: PaginationLinks;
  currentPage: number;
  totalPages: number;
}

function parseLinkHeader(linkHeader: string): PaginationLinks {
  const links: PaginationLinks = {};

  if (!linkHeader) return links;

  const linkPattern = /<([^>]+)>;\s*rel="([^"]+)"/g;
  let match;

  while ((match = linkPattern.exec(linkHeader)) !== null) {
    const url = match[1];
    const relation = match[2];

    switch (relation.toLowerCase()) {
      case "first":
        links.first = url;
        break;
      case "prev":
      case "previous":
        links.previous = url;
        break;
      case "next":
        links.next = url;
        break;
      case "last":
        links.last = url;
        break;
    }
  }

  return links;
}

const PAGE_REGEX = /[?&]page=(\d+)/;

// Extract page number from URL
function extractPageFromUrl(url: string): number {
  const match = PAGE_REGEX.exec(url);
  return match ? parseInt(match[1], 10) : 1;
}

function convertToLocalUrl(url: string | undefined, sortBy: number): string | undefined {
  if (!url) return undefined;

  const pageMatch = PAGE_REGEX.exec(url);
  const page = pageMatch ? pageMatch[1] : "1";

  return `?sortBy=${sortBy}&page=${page}`;
}

// Generate pagination items from Link header info
function generatePaginationItems(
  linkPagination: LinkBasedPagination,
  sortBy: number
): Array<{ number: number; href: string; current: boolean }> {
  const { currentPage, totalPages } = linkPagination;
  const items = [];
  const maxVisiblePages = 10;

  if (totalPages <= maxVisiblePages) {
    // Show all pages if total is small
    for (let i = 1; i <= totalPages; i++) {
      items.push({
        number: i,
        href: `?sortBy=${sortBy}&page=${i}`,
        current: i === currentPage,
      });
    }
  } else {
    // Smart truncation for many pages
    let startPage = Math.max(1, currentPage - 3);
    let endPage = Math.min(totalPages, currentPage + 3);

    if (currentPage <= 4) {
      endPage = Math.min(maxVisiblePages, totalPages);
    } else if (currentPage >= totalPages - 3) {
      startPage = Math.max(1, totalPages - maxVisiblePages + 1);
    }

    // Always show first page
    if (startPage > 1) {
      items.push({
        number: 1,
        href: `?sortBy=${sortBy}&page=1`,
        current: false,
      });

      if (startPage > 2) {

        items.push({
          number: -1,
          href: '#',
          current: false,
        });
      }
    }

    // Add visible pages
    for (let i = startPage; i <= endPage; i++) {
      items.push({
        number: i,
        href: `?sortBy=${sortBy}&page=${i}`,
        current: i === currentPage,
      });
    }

    // Always show last page
    if (endPage < totalPages) {
      if (endPage < totalPages - 1) {
        items.push({
          number: -1,
          href: '#',
          current: false,
        });
      }

      items.push({
        number: totalPages,
        href: `?sortBy=${sortBy}&page=${totalPages}`,
        current: false,
      });
    }
  }

  return items;
}

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
  const sortBy = resolvedSearchParams.sortBy === "1" ? 1 : 0;
  const currentPage = parseInt(resolvedSearchParams.page || "1", 10);

  const sortOptions = [
    {
      value: "0",
      label: "Date exception created (newest first)",
    },
    {
      value: "1",
      label: "Date exception created (oldest first)",
    },
  ];

  try {
    const response = await fetchExceptions({sortOrder: sortBy,page: currentPage,isReport: true});

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

    const linkHeader = response.headers?.get('Link') || response.linkHeader;
    const paginationLinks = parseLinkHeader(linkHeader || '');

    let totalPages = response.data.TotalPages;
    let detectedCurrentPage = currentPage;

    if (paginationLinks.last) {
      totalPages = extractPageFromUrl(paginationLinks.last);
    }

    if (paginationLinks.next && !paginationLinks.previous) {
      detectedCurrentPage = 1;
    } else if (paginationLinks.previous && !paginationLinks.next) {
      detectedCurrentPage = totalPages;
    } else if (paginationLinks.next) {
      detectedCurrentPage = extractPageFromUrl(paginationLinks.next) - 1;
    }

    const linkBasedPagination: LinkBasedPagination = {
      links: paginationLinks,
      currentPage: detectedCurrentPage,
      totalPages: totalPages,
    };

    const paginationItems = generatePaginationItems(linkBasedPagination, sortBy);

    // Calculate the range of items being shown using PageSize from API response
    const startItem = (currentPage - 1) * response.data.PageSize + 1;
    const endItem = Math.min(startItem + response.data.Items.length - 1, response.data.TotalItems);

    return (
      <>
        <Breadcrumb items={breadcrumbItems} />
        <main className="nhsuk-main-wrapper" id="maincontent" role="main">
          <div className="nhsuk-grid-row">
            <div className="nhsuk-grid-column-full">
              <h1 data-testid="heading-not-raised">
                Not raised breast screening exceptions
              </h1>

              <div className="app-form-results-container">
                <SortExceptionsForm
                  sortBy={sortBy}
                  options={sortOptions}
                  hiddenText="not raised exceptions"
                  testId="sort-not-raised-exceptions"
                />
                <p
                  className="app-results-text"
                  data-testid="not-raised-exception-count"
                >
                  Showing {startItem} to {endItem} of {response.data.TotalItems} results
                </p>
              </div>

              <div className="nhsuk-card nhsuk-u-margin-bottom-5">
                <div className="nhsuk-card__content">
                  <ExceptionsTable exceptions={exceptionDetails} />
                </div>
              </div>

              {totalPages > 1 && (
                <Pagination
                  items={paginationItems}
                  previous={
                    paginationLinks.previous
                      ? { href: convertToLocalUrl(paginationLinks.previous, sortBy)! }
                      : undefined
                  }
                  next={
                    paginationLinks.next
                      ? { href: convertToLocalUrl(paginationLinks.next, sortBy)! }
                      : undefined
                  }
                />
              )}
            </div>
          </div>
        </main>
      </>
    );
  } catch {
    return (
      <>
        <Breadcrumb items={breadcrumbItems} />
        <DataError />
      </>
    );
  }
}
