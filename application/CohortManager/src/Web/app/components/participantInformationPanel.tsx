import { ExceptionDetails } from "@/app//types";
import {
  formatDate,
  formatCompactDate,
  formatNhsNumber,
  formatGenderValue,
} from "@/app/lib/utils";
import { updateExceptions } from "@/app/lib/updateExceptions";

interface ParticipantInformationPanelProps {
  readonly exceptionDetails: ExceptionDetails;
  readonly isEditMode: boolean;
  readonly searchParams?: { [key: string]: string | string[] | undefined };
}

export default function ParticipantInformationPanel({
  exceptionDetails,
  isEditMode,
  searchParams,
}: Readonly<ParticipantInformationPanelProps>) {
  const updateExceptionsWithId = updateExceptions.bind(
    null,
    exceptionDetails.exceptionId
  );

  const validationError = searchParams?.error as string;
  const hasError = !!validationError;

  return (
    <>
      {hasError && (
        <div
          className="nhsuk-error-summary"
          aria-labelledby="error-summary-title"
          role="alert"
          tabIndex={-1}
        >
          <h2 className="nhsuk-error-summary__title" id="error-summary-title">
            There is a problem
          </h2>
          <div className="nhsuk-error-summary__body">
            <ul className="nhsuk-list nhsuk-error-summary__list" role="list">
              <li>
                <a href="#service-now-case-id">{validationError}</a>
              </li>
            </ul>
          </div>
        </div>
      )}

      <div
        className={
          !exceptionDetails.serviceNowId || isEditMode
            ? "nhsuk-card nhsuk-do-dont-list nhsuk-u-margin-bottom-4"
            : "nhsuk-card app-card nhsuk-u-margin-bottom-4"
        }
      >
        {(!exceptionDetails.serviceNowId || isEditMode) && (
          <span className="nhsuk-do-dont-list__label nhsuk-u-font-weight-bold">
            {exceptionDetails.portalFormTitle ||
              "Portal form: Request to amend incorrect patient PDS record data"}
          </span>
        )}
        <h2>Participant details</h2>

        <dl
          className="nhsuk-summary-list"
          data-testid="participant-details-section"
        >
          <div className="nhsuk-summary-list__row">
            <dt className="nhsuk-summary-list__key">NHS number</dt>
            <dd className="nhsuk-summary-list__value">
              {formatNhsNumber(exceptionDetails.nhsNumber ?? "")}
            </dd>
          </div>
          {exceptionDetails.supersededByNhsNumber && (
            <div className="nhsuk-summary-list__row">
              <dt className="nhsuk-summary-list__key">
                Superseded by NHS number
              </dt>
              <dd className="nhsuk-summary-list__value">
                {formatNhsNumber(exceptionDetails.supersededByNhsNumber)}
              </dd>
            </div>
          )}
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
              {formatGenderValue(exceptionDetails.gender)}
            </dd>
          </div>
          <div className="nhsuk-summary-list__row">
            <dt className="nhsuk-summary-list__key">Current address</dt>
            <dd className="nhsuk-summary-list__value">
              {exceptionDetails.address}
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
          <dl
            className="nhsuk-summary-list"
            data-testid="exception-details-section"
          >
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
                <p>
                  {exceptionDetails.moreDetails ||
                    exceptionDetails.shortDescription}
                </p>
                {exceptionDetails.reportingId && (
                  <p>
                    Cohort Manager rule (to be included for reporting):{" "}
                    {exceptionDetails.reportingId}
                  </p>
                )}
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
      {(isEditMode || !exceptionDetails.serviceNowId) && (
        <div className="nhsuk-card nhsuk-u-margin-bottom-4">
          <div className="nhsuk-card__content">
            <h2 id="exception-status">Exception status</h2>
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
            <form action={updateExceptionsWithId}>
              <div
                className={`nhsuk-form-group ${
                  hasError ? "nhsuk-form-group--error" : ""
                }`}
              >
                <label
                  className="nhsuk-label nhsuk-u-font-weight-bold"
                  htmlFor="service-now-case-id"
                  data-testid="service-now-case-id-label"
                >
                  Enter ServiceNow Case ID
                </label>
                <div className="nhsuk-hint" id="service-now-case-id-hint">
                  For example, CS1234567
                </div>
                {hasError && (
                  <span
                    className="nhsuk-error-message"
                    id="service-now-case-id-error"
                  >
                    <span className="nhsuk-u-visually-hidden">Error:</span>
                    {validationError}
                  </span>
                )}
                <input
                  className={`nhsuk-input nhsuk-input--width-10 ${
                    hasError ? "nhsuk-input--error" : ""
                  }`}
                  data-testid="service-now-case-id-input"
                  id="service-now-case-id"
                  name="serviceNowID"
                  type="text"
                  aria-describedby={`service-now-case-id-hint ${
                    hasError ? "service-now-case-id-error" : ""
                  }`}
                  defaultValue={exceptionDetails.serviceNowId ?? ""}
                />
                <input
                  type="hidden"
                  name="isEditMode"
                  value={isEditMode ? "true" : "false"}
                />
              </div>
              <button
                className="nhsuk-button"
                data-module="nhsuk-button"
                type="submit"
                data-testid="save-continue-button"
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
