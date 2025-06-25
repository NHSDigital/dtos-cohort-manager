import { ExceptionDetails } from "@/app//types";
import {
  formatDate,
  formatCompactDate,
  formatNhsNumber,
  formatPhoneNumber,
} from "@/app/lib/utils";

interface ParticipantInformationPanelProps {
  readonly exceptionDetails: ExceptionDetails;
}

export default function ParticipantInformationPanel({
  exceptionDetails,
}: Readonly<ParticipantInformationPanelProps>) {
  return (
    <>
      <div
        className={
          !exceptionDetails.serviceNowId
            ? "nhsuk-card nhsuk-do-dont-list nhsuk-u-margin-bottom-4"
            : "nhsuk-card app-card nhsuk-u-margin-bottom-4"
        }
      >
        {!exceptionDetails.serviceNowId && (
          <span className="nhsuk-do-dont-list__label nhsuk-u-font-weight-bold">
            Portal form: Request to amend incorrect patient PDS record data
          </span>
        )}
        <h2>Participant details</h2>

        <dl className="nhsuk-summary-list">
          <div className="nhsuk-summary-list__row">
            <dt className="nhsuk-summary-list__key">NHS number</dt>
            <dd className="nhsuk-summary-list__value">
              {formatNhsNumber(exceptionDetails.nhsNumber ?? "")}
            </dd>
          </div>
          <div className="nhsuk-summary-list__row">
            <dt className="nhsuk-summary-list__key">Surname</dt>
            <dd className="nhsuk-summary-list__value">
              {exceptionDetails.surname}
            </dd>
          </div>
          <div className="nhsuk-summary-list__row">
            <dt className="nhsuk-summary-list__key">Forename</dt>
            <dd className="nhsuk-summary-list__value">
              {exceptionDetails.forename}
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
            <dt className="nhsuk-summary-list__key">
              Registered practice code
            </dt>
            <dd className="nhsuk-summary-list__value">
              {exceptionDetails.primaryCareProvider}
            </dd>
          </div>
        </dl>
      </div>
      <div className="nhsuk-card nhsuk-u-margin-bottom-4">
        <div className="nhsuk-card__content">
          <h2>Exception details</h2>
          <dl className="nhsuk-summary-list">
            <div className="nhsuk-summary-list__row">
              <dt className="nhsuk-summary-list__key">
                Date exception created
              </dt>
              <dd className="nhsuk-summary-list__value">
                {formatDate(exceptionDetails.dateCreated)}
              </dd>
            </div>
            <div className="nhsuk-summary-list__row">
              <dt className="nhsuk-summary-list__key">More detail</dt>
              <dd className="nhsuk-summary-list__value">
                {exceptionDetails.shortDescription}
              </dd>
            </div>
            {!exceptionDetails.serviceNowId && (
              <div className="nhsuk-summary-list__row">
                <dt className="nhsuk-summary-list__key">ServiceNow ID</dt>
                <dd className="nhsuk-summary-list__value">
                  <strong className="nhsuk-tag nhsuk-tag--grey">
                    Not raised
                  </strong>
                </dd>
              </div>
            )}
          </dl>
        </div>
      </div>
      {exceptionDetails.serviceNowId ? null : (
        <div className="nhsuk-card nhsuk-u-margin-bottom-4">
          <div className="nhsuk-card__content">
            <h2>Exception status</h2>
            <p>
              Entering a ServiceNow case ID will change the status of the
              exception to “Raised”. <br />
              You’ll be able to access it on the “Raised breast screening
              exceptions” page.
            </p>
            <p>
              If you need to change the exception status back to “Not raised”,
              delete any case ID previously entered and then save the changes
              with this field left empty.
            </p>
            <form>
              <div className="nhsuk-form-group">
                <label
                  className="nhsuk-label nhsuk-u-font-weight-bold"
                  htmlFor="service-now-case-id"
                >
                  Enter ServiceNow Case ID
                </label>
                <div className="nhsuk-hint" id="service-now-case-id-hint">
                  For example, CS1234567
                </div>
                <input
                  className="nhsuk-input nhsuk-input--width-10"
                  id="service-now-case-id"
                  name="service-now-case-id"
                  type="text"
                  aria-describedby="service-now-case-id-hint"
                  defaultValue={exceptionDetails.serviceNowId || ""}
                />
              </div>
              <button
                className="nhsuk-button"
                data-module="nhsuk-button"
                type="submit"
              >
                Save and continue
              </button>
            </form>
          </div>
        </div>
      )}
    </>
  );
}
