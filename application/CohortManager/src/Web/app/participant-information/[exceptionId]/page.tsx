import type { Metadata } from "next";
import { ExceptionDetails } from "@/app/types";
import { auth } from "@/app/lib/auth";
import { fetchExceptions } from "@/app/lib/fetchExceptions";
import { getIsCohortManager } from "@/app/lib/access";
import { formatDate } from "@/app/lib/utils";
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
}) {
  const session = await auth();
  const isCohortManager = await getIsCohortManager(session);

  if (!isCohortManager) {
    return <Unauthorised />;
  }

  const params = await props.params;
  const exceptionId = Number(params.exceptionId);

  try {
    const exception = await fetchExceptions(exceptionId);

    const exceptionDetails: ExceptionDetails = {
      exceptionId: exceptionId,
      nhsNumber: exception.NhsNumber,
      surname: exception.ExceptionDetails.FamilyName,
      forename: exception.ExceptionDetails.GivenName,
      dateCreated: exception.DateCreated,
      shortDescription: exception.RuleDescription,
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
      contactDetails: {
        phoneNumber: exception.ExceptionDetails.TelephoneNumberHome,
        email: exception.ExceptionDetails.EmailAddressHome,
      },
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
              {exceptionDetails.serviceNowId && (
                <dl className="nhsuk-summary-list">
                  <div className="nhsuk-summary-list__row">
                    <dt className="nhsuk-summary-list__key">
                      Portal form used
                    </dt>
                    <dd className="nhsuk-summary-list__value">
                      Request to amend incorrect patient PDS record data
                    </dd>
                    <dd className="nhsuk-summary-list__actions"></dd>
                  </div>
                  <div className="nhsuk-summary-list__row">
                    <dt className="nhsuk-summary-list__key">
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
                    <dt className="nhsuk-summary-list__key">
                      ServiceNow Case ID
                    </dt>
                    <dd className="nhsuk-summary-list__value">
                      {exceptionDetails.serviceNowId}
                    </dd>
                    <dd className="nhsuk-summary-list__actions">
                      <a href="#">
                        Change{" "}
                        <span className="nhsuk-u-visually-hidden">
                          ServiceNow Case ID
                        </span>
                      </a>
                    </dd>
                  </div>
                </dl>
              )}
              <ParticipantInformationPanel
                exceptionDetails={exceptionDetails}
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
