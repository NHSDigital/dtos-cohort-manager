import { ExceptionAPIDetails } from "@/app/types/exceptionsApi";
import { formatCompactDate, formatNhsNumber } from "@/app/lib/utils";

interface ReportsInformationTableProps {
  readonly category: number;
  readonly items: readonly ExceptionAPIDetails[];
}

export default function ReportsInformationTable({
  category,
  items,
}: Readonly<ReportsInformationTableProps>) {
  const isConfusion = category === 12; // Possible confusion
  return (
    <table role="table" className="nhsuk-table-responsive">
      <thead className="nhsuk-table__head">
        {isConfusion ? (
          <tr role="row">
            <th role="columnheader" scope="col">
              Patient name
            </th>
            <th role="columnheader" scope="col">
              Date of Birth
            </th>
            <th role="columnheader" scope="col">
              NHS number
            </th>
          </tr>
        ) : (
          <tr role="row">
            <th role="columnheader" scope="col">
              Patient name
            </th>
            <th role="columnheader" scope="col">
              Date of Birth
            </th>
            <th role="columnheader" scope="col">
              NHS number
            </th>
            <th role="columnheader" scope="col">
              Superseded by NHS number
            </th>
          </tr>
        )}
      </thead>
      <tbody className="nhsuk-table__body">
        {items.map((item) => {
          const d = item.ExceptionDetails;
          const name = `${d.GivenName} ${d.FamilyName}`.trim();
          return (
            <tr role="row" className="nhsuk-table__row" key={item.ExceptionId}>
              <td className="nhsuk-table__cell">
                <span
                  className="nhsuk-table-responsive__heading"
                  aria-hidden="true"
                >
                  Patient name{" "}
                </span>
                {name}
              </td>
              <td className="nhsuk-table__cell app-u-no-wrap">
                <span
                  className="nhsuk-table-responsive__heading"
                  aria-hidden="true"
                >
                  Date of Birth{" "}
                </span>
                {formatCompactDate(d.DateOfBirth)}
              </td>
              {isConfusion ? (
                <td className="nhsuk-table__cell app-u-no-wrap">
                  <span
                    className="nhsuk-table-responsive__heading"
                    aria-hidden="true"
                  >
                    NHS number{" "}
                  </span>
                  {formatNhsNumber(item.NhsNumber)}
                </td>
              ) : (
                <>
                  <td className="nhsuk-table__cell app-u-no-wrap">
                    <span
                      className="nhsuk-table-responsive__heading"
                      aria-hidden="true"
                    >
                      NHS number{" "}
                    </span>
                    {formatNhsNumber(item.NhsNumber)}
                  </td>
                  <td className="nhsuk-table__cell app-u-no-wrap">
                    <span
                      className="nhsuk-table-responsive__heading"
                      aria-hidden="true"
                    >
                      Superseded by NHS number{" "}
                    </span>
                    {formatNhsNumber(d.SupersededByNhsNumber ?? "")}
                  </td>
                </>
              )}
            </tr>
          );
        })}
      </tbody>
    </table>
  );
}
