import CardGroup from "@/app/components/cardGroup";
import DataError from "@/app/components/dataError";
import { fetchExceptions } from "@/app/lib/fetchExceptions";

export default async function OverviewData() {
  try {
    const [notRaisedExceptions, raisedExceptions] = await Promise.all([
      fetchExceptions({ exceptionStatus: 2 }),
      fetchExceptions({ exceptionStatus: 1 }),
    ]);

    const exceptionItems = [
      {
        value: notRaisedExceptions.data.TotalItems,
        label: "Not raised",
        description: "Exceptions to be raised with teams",
        url: "/exceptions",
      },
      {
        value: raisedExceptions.data.TotalItems,
        label: "Raised",
        description: "Access and amend previously raised exceptions",
        url: "/exceptions/raised",
      },
    ];

    return <CardGroup items={exceptionItems} />;
  } catch {
    return <DataError />;
  }
}
