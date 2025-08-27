import Link from "next/link";
import { ReportDetails } from "@/app/types";
import { formatDate } from "@/app/lib/utils";

interface ReportTableProps {
  readonly reports: readonly ReportDetails[];
  readonly caption?: string;
}

export default function ExceptionsTable({
  reports,
  caption,
}: Readonly<ReportTableProps>) {
  if (!reports || reports.length === 0) {
    return <p>There are currently no reports.</p>;
  }

  return (
    <table role="table" className="nhsuk-table-responsive">
      <caption className="nhsuk-table__caption nhsuk-u-visually-hidden">
        {caption ? `${caption}` : "All reports"}
      </caption>
      <thead className="nhsuk-table__head">
        <tr role="row">
          <th role="columnheader" scope="col">
            Date
          </th>
          <th role="columnheader" scope="col">
            Demographic change
          </th>
          <th role="columnheader" scope="col">
            Action
          </th>
        </tr>
      </thead>
      <tbody className="nhsuk-table__body">
        {reports.map((exception) => {
          const categoryTitle =
            exception.category === 12
              ? "Possible confusion"
              : exception.category === 13
              ? "NHS number change"
              : String(exception.category);
          return (
            <tr
              role="row"
              className="nhsuk-table__row"
              key={exception.reportId}
            >
              <td className="nhsuk-table__cell app-u-no-wrap">
                <span
                  className="nhsuk-table-responsive__heading"
                  aria-hidden="true"
                >
                  Date{" "}
                </span>
                {formatDate(exception.dateCreated)}
              </td>
              <td className="nhsuk-table__cell">
                <span
                  className="nhsuk-table-responsive__heading"
                  aria-hidden="true"
                >
                  Demographic change{" "}
                </span>
                {categoryTitle}
              </td>
              <td className="nhsuk-table__cell">
                <span
                  className="nhsuk-table-responsive__heading"
                  aria-hidden="true"
                >
                  Action{" "}
                </span>
                <Link href={`/reports/${exception.reportId}`}>View report</Link>
              </td>
            </tr>
          );
        })}
      </tbody>
    </table>
  );
}
