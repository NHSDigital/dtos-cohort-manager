import type { Metadata } from "next";
import { ExceptionDetails } from "@/app/types";
import { auth } from "@/app/lib/auth";
import { fetchExceptions } from "@/app/lib/fetchExceptions";
import { canAccessCohortManager } from "@/app/lib/access";
import { formatDate } from "@/app/lib/utils";
import { getRuleMapping } from "@/app/lib/ruleMapping";
import Breadcrumb from "@/app/components/breadcrumb";
import ParticipantInformationPanel from "@/app/components/participantInformationPanel";
import Unauthorised from "@/app/components/unauthorised";
import DataError from "@/app/components/dataError";

export const metadata: Metadata = {
  title: `Exception information - ${process.env.SERVICE_NAME} - NHS`,
};

export default async function Page(props: {
  readonly params: Promise<{
    readonly exceptionId: string;
  }>;
  readonly searchParams?: Promise<{
    readonly edit?: string;
    readonly error?: string;
  }>;
}) {
  const session = await auth();
  const isCohortManager = await canAccessCohortManager(session);

  if (!isCohortManager) {
    return <Unauthorised />;
  }

  const params = await props.params;
  const exceptionId = Number(params.exceptionId);
  const resolvedSearchParams = props.searchParams
    ? await props.searchParams
    : {};
  const isEditMode = resolvedSearchParams.edit === "true";

  try {
    const { data: exception } = await fetchExceptions({ exceptionId });
    const ruleMapping = getRuleMapping(
      exception.RuleId,
      exception.RuleDescription
    );

    const exceptionDetails: ExceptionDetails = {
      exceptionId: exceptionId,
      nhsNumber: exception.NhsNumber,
      supersededByNhsNumber: exception.ExceptionDetails.SupersededByNhsNumber,
      surname: exception.ExceptionDetails.FamilyName,
      forename: exception.ExceptionDetails.GivenName,
      dateCreated: exception.DateCreated,
      shortDescription: ruleMapping.ruleDescription,
      moreDetails: ruleMapping.moreDetails,
      reportingId: ruleMapping.reportingId,
      portalFormTitle: ruleMapping.portalFormTitle,
      dateOfBirth: exception.ExceptionDetails.DateOfBirth,
      gender: exception.ExceptionDetails.Gender,
      address: `${exception.ExceptionDetails.ParticipantAddressLine1}${
        exception.ExceptionDetails.ParticipantAddressLine2
          ? `, ${exception.ExceptionDetails.ParticipantAddressLine2}`
          : ""
      }${
        exception.ExceptionDetails.ParticipantAddressLine3
          ? `, ${exception.ExceptionDetails.ParticipantAddressLine3}`
          : ""
      }${
        exception.ExceptionDetails.ParticipantAddressLine4
          ? `, ${exception.ExceptionDetails.ParticipantAddressLine4}`
          : ""
      }${
        exception.ExceptionDetails.ParticipantAddressLine5
          ? `, ${exception.ExceptionDetails.ParticipantAddressLine5}`
          : ""
      }, ${exception.ExceptionDetails.ParticipantPostCode}`,
      primaryCareProvider: exception.ExceptionDetails.PrimaryCareProvider,
      serviceNowId: exception.ServiceNowId ?? "",
      serviceNowCreatedDate: exception.ServiceNowCreatedDate,
    };

    const breadcrumbItems = [
      {
        label: "Home",
        url: "/",
      },
      {
        label: exceptionDetails.serviceNowId
          ? "Raised breast screening exceptions"
          : "Not raised breast screening exceptions",
        url: exceptionDetails.serviceNowId
          ? "/exceptions/raised"
          : "/exceptions",
      },
    ];

    return (
      <>
        <Breadcrumb items={breadcrumbItems} />
        <main className="nhsuk-main-wrapper" id="maincontent" role="main">
          <div className="nhsuk-grid-row">
            <div className="nhsuk-grid-column-full">
              <h1>
                Exception information{" "}
                <span className="nhsuk-caption-xl">
                  Local reference (exception ID): {exceptionDetails.exceptionId}
                </span>
              </h1>
              {exceptionDetails.serviceNowId && !isEditMode && (
                <dl
                  className="nhsuk-summary-list"
                  data-testid="exception-details-labels"
                >
                  <div className="nhsuk-summary-list__row">
                    <dt
                      className="nhsuk-summary-list__key"
                      data-testid="portal-form-used-label"
                    >
                      Portal form used
                    </dt>
                    <dd className="nhsuk-summary-list__value">
                      {exceptionDetails.portalFormTitle ||
                        "Request to amend incorrect patient PDS record data"}
                    </dd>
                    <dd className="nhsuk-summary-list__actions"></dd>
                  </div>
                  <div className="nhsuk-summary-list__row">
                    <dt
                      className="nhsuk-summary-list__key"
                      data-testid="exception-status-label"
                    >
                      Exception status
                    </dt>
                    <dd className="nhsuk-summary-list__value">
                      {exceptionDetails.serviceNowId ? (
                        <>
                          <strong className="nhsuk-tag">Raised</strong> on{" "}
                          {formatDate(
                            exceptionDetails.serviceNowCreatedDate ?? ""
                          )}
                        </>
                      ) : (
                        <strong className="nhsuk-tag nhsuk-tag--grey">
                          Not raised
                        </strong>
                      )}
                    </dd>
                    <dd className="nhsuk-summary-list__actions"></dd>
                  </div>
                  <div className="nhsuk-summary-list__row">
                    <dt
                      className="nhsuk-summary-list__key"
                      data-testid="Service-now-case-label"
                    >
                      ServiceNow Case ID
                    </dt>
                    <dd className="nhsuk-summary-list__value">
                      {exceptionDetails.serviceNowId}
                    </dd>
                    <dd
                      className="nhsuk-summary-list__actions"
                      data-testid="change-link"
                    >
                      <a href="?edit=true#exception-status">
                        Change{" "}
                        <span className="nhsuk-u-visually-hidden">
                          ServiceNow Case ID
                          {isEditMode}
                        </span>
                      </a>
                    </dd>
                  </div>
                </dl>
              )}
              <ParticipantInformationPanel
                exceptionDetails={exceptionDetails}
                isEditMode={isEditMode}
                searchParams={resolvedSearchParams}
              />
            </div>
          </div>
        </main>
      </>
    );
  } catch (error) {
    console.error("Error fetching exception details:", error);
    return (
      <>
        <Breadcrumb items={[{ label: "Home", url: "/" }]} />
        <DataError />
      </>
    );
  }
}
