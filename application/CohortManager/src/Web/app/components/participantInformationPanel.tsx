import { ExceptionDetails } from "@/app//types";
import {
  formatDate,
  formatCompactDate,
  formatNhsNumber,
  formatPhoneNumber,
} from "@/app/lib/utils";

interface ParticipantInformationPanelProps {
  exceptionDetails: ExceptionDetails;
}

export default function ParticipantInformationPanel({
  exceptionDetails,
}: ParticipantInformationPanelProps) {
  return (
    <>
      <div className="nhsuk-card nhsuk-u-margin-bottom-4">
        <div className="nhsuk-card__content">
          <h2 className="nhsuk-heading-m">Exception details</h2>
          <dl className="nhsuk-summary-list">
            <div className="nhsuk-summary-list__row">
              <dt className="nhsuk-summary-list__key">Date created</dt>
              <dd className="nhsuk-summary-list__value">
                {formatDate(exceptionDetails.dateCreated)}
              </dd>
            </div>
            <div className="nhsuk-summary-list__row">
              <dt className="nhsuk-summary-list__key">Short description</dt>
              <dd className="nhsuk-summary-list__value">
                {exceptionDetails.shortDescription}
              </dd>
            </div>
          </dl>
        </div>
      </div>
      <div className="nhsuk-card">
        <div className="nhsuk-card__content">
          <h2 className="nhsuk-heading-m">Personal details</h2>
          <dl className="nhsuk-summary-list">
            <div className="nhsuk-summary-list__row">
              <dt className="nhsuk-summary-list__key">NHS number</dt>
              <dd className="nhsuk-summary-list__value">
                {formatNhsNumber(exceptionDetails.nhsNumber ?? "")}
              </dd>
            </div>
            <div className="nhsuk-summary-list__row">
              <dt className="nhsuk-summary-list__key">Name</dt>
              <dd className="nhsuk-summary-list__value">
                {exceptionDetails.name}
              </dd>
            </div>
            <div className="nhsuk-summary-list__row">
              <dt className="nhsuk-summary-list__key">Date of birth</dt>
              <dd className="nhsuk-summary-list__value">
                {formatCompactDate(exceptionDetails.dateOfBirth ?? "")}
              </dd>
            </div>
            <div className="nhsuk-summary-list__row">
              <dt className="nhsuk-summary-list__key">Gender</dt>
              <dd className="nhsuk-summary-list__value">
                {exceptionDetails.gender}
              </dd>
            </div>
            <div className="nhsuk-summary-list__row">
              <dt className="nhsuk-summary-list__key">Current address</dt>
              <dd className="nhsuk-summary-list__value">
                {exceptionDetails.address}
              </dd>
            </div>
            <div className="nhsuk-summary-list__row">
              <dt className="nhsuk-summary-list__key">Contact details</dt>
              <dd className="nhsuk-summary-list__value">
                <p>
                  {formatPhoneNumber(
                    exceptionDetails.contactDetails?.phoneNumber ?? ""
                  )}
                </p>
                <p>{exceptionDetails.contactDetails?.email}</p>
              </dd>
            </div>
            <div className="nhsuk-summary-list__row">
              <dt className="nhsuk-summary-list__key">GP practice code</dt>
              <dd className="nhsuk-summary-list__value">
                {exceptionDetails.gpPracticeCode}
              </dd>
            </div>
            <div className="nhsuk-summary-list__row">
              <dt className="nhsuk-summary-list__key">GP practice address</dt>
              <dd className="nhsuk-summary-list__value">
                {exceptionDetails.gpPracticeAddress}
              </dd>
            </div>
          </dl>
        </div>
      </div>
    </>
  );
}
