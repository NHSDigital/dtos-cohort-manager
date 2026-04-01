import { Suspense } from "react";
import CardGroup from "@/app/components/cardGroup";
import OverviewData from "@/app/components/overviewData";

const skeletonExceptionItems = [
  {
    label: "Not raised",
    description: "Exceptions to be raised with teams",
    url: "/exceptions",
    loading: true,
  },
  {
    label: "Raised",
    description: "Access and amend previously raised exceptions",
    url: "/exceptions/raised",
    loading: true,
  },
];

const reportItems = [
  {
    value: 28,
    label: "Reports",
    description: "To manage investigations into demographic changes",
    url: "/reports",
  },
];

const dummyGpCodeItems = [
  {
    label: "Remove Dummy GP Code",
    description: "Remove a dummy GP practice code from a participant record",
    url: "/remove-dummy-gp-code",
  },
];

export default function Overview() {
  return (
    <main className="nhsuk-main-wrapper" id="maincontent" role="main">
      <div className="nhsuk-grid-row">
        <div className="nhsuk-grid-column-full">
          <h1>Breast screening</h1>
          <h2>Exceptions</h2>
          <Suspense fallback={<CardGroup items={skeletonExceptionItems} />}>
            <OverviewData />
          </Suspense>
          <h2>Reports</h2>
          <CardGroup items={reportItems} />
          <h2>Dummy GP Code</h2>
          <CardGroup items={dummyGpCodeItems} />
        </div>
      </div>
    </main>
  );
}
