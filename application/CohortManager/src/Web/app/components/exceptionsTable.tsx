import Link from "next/link";
import { ExceptionDetails } from "@/app/types";
import { formatNhsNumber, formatDate } from "@/app/lib/utils";

interface ExceptionsTableProps {
  readonly exceptions: readonly ExceptionDetails[];
  readonly caption?: string;
}

export default function ExceptionsTable({
  exceptions,
  caption,
}: Readonly<ExceptionsTableProps>) {
  return (
    <table role="table" className="nhsuk-table-responsive">
      <caption className="nhsuk-table__caption">
        {caption ? `${caption}` : "Total breast screening exceptions"}
      </caption>
      <thead className="nhsuk-table__head">
        <tr role="row">
          <th role="columnheader" scope="col">
            Exception ID
          </th>
          <th role="columnheader" scope="col">
            NHS number
          </th>
          <th role="columnheader" scope="col">
            Date created
          </th>
          <th role="columnheader" scope="col">
            Short description
          </th>
        </tr>
      </thead>
      <tbody className="nhsuk-table__body">
        {exceptions.map((exception) => (
          <tr
            role="row"
            className="nhsuk-table__row"
            key={exception.exceptionId}
          >
            <td className="nhsuk-table__cell">
              <span
                className="nhsuk-table-responsive__heading"
                aria-hidden="true"
              >
                Exception ID{" "}
              </span>
              <Link href={`/participant-information/${exception.exceptionId}`}>
                {exception.exceptionId}
              </Link>
            </td>
            <td className="nhsuk-table__cell app-u-no-wrap">
              <span
                className="nhsuk-table-responsive__heading"
                aria-hidden="true"
              >
                NHS number{" "}
              </span>
              {formatNhsNumber(exception.nhsNumber ?? "")}
            </td>
            <td className="nhsuk-table__cell app-u-no-wrap">
              <span
                className="nhsuk-table-responsive__heading"
                aria-hidden="true"
              >
                Date created{" "}
              </span>
              {formatDate(exception.dateCreated)}
            </td>
            <td className="nhsuk-table__cell">
              <span
                className="nhsuk-table-responsive__heading"
                aria-hidden="true"
              >
                Short description{" "}
              </span>
              {exception.shortDescription}
            </td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}
