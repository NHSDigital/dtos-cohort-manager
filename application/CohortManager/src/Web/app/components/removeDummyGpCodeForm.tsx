"use client";

import { useActionState, useEffect, useState } from "react";
import { removeDummyGpCode, type RemoveDummyGpCodeState } from "@/app/lib/removeDummyGpCode";

export default function RemoveDummyGpCodeForm() {
  const [state, formAction] = useActionState<RemoveDummyGpCodeState, FormData>(removeDummyGpCode, null);
  const [formKey, setFormKey] = useState(0);

  useEffect(() => {
    if (state?.error) {
      setFormKey((prev) => prev + 1);
    }
  }, [state]);

  const hasError = !!state?.error;
  const errorField = state?.field;
  const errorMessage = state?.error;
  const values = state?.values;

  return (
    <>
      {hasError && (
        <div className="nhsuk-error-summary" aria-labelledby="error-summary-title" role="alert" tabIndex={-1} data-testid="error-summary">
          <h2 className="nhsuk-error-summary__title" id="error-summary-title">There is a problem</h2>
          <div className="nhsuk-error-summary__body">
            <ul className="nhsuk-list nhsuk-error-summary__list">
              <li>
                {errorField ? (
                  <a href={`#${errorField}`}>{errorMessage}</a>
                ) : (
                  <span>{errorMessage}</span>
                )}
              </li>
            </ul>
          </div>
        </div>
      )}

      <h1>Remove Dummy GP Code</h1>
      <form action={formAction} key={formKey}>
        <div className={`nhsuk-form-group ${hasError && errorField === "nhsNumber" ? "nhsuk-form-group--error" : ""}`}>
          <label className="nhsuk-label nhsuk-u-font-weight-bold" htmlFor="nhsNumber">NHS Number</label>
          <div className="nhsuk-hint" id="nhsNumber-hint">A 10 digit number, for example 485 777 3456</div>
          {hasError && errorField === "nhsNumber" && (
            <span className="nhsuk-error-message" id="nhsNumber-error">
              <span className="nhsuk-u-visually-hidden">Error:</span>{" "}
              {errorMessage}
            </span>
          )}
          <input
            className={`nhsuk-input nhsuk-input--width-10 ${hasError && errorField === "nhsNumber" ? "nhsuk-input--error" : ""}`}
            id="nhsNumber"
            name="nhsNumber"
            type="text"
            inputMode="numeric"
            defaultValue={values?.nhsNumber}
            aria-describedby={`nhsNumber-hint${hasError && errorField === "nhsNumber" ? " nhsNumber-error" : ""}`}
            data-testid="nhs-number-input"
          />
        </div>

        <div className={`nhsuk-form-group ${hasError && errorField === "forename" ? "nhsuk-form-group--error" : ""}`}>
          <label className="nhsuk-label nhsuk-u-font-weight-bold" htmlFor="forename">Forename</label>
          {hasError && errorField === "forename" && (
            <span className="nhsuk-error-message" id="forename-error">
              <span className="nhsuk-u-visually-hidden">Error:</span>{" "}
              {errorMessage}
            </span>
          )}
          <input className={`nhsuk-input ${hasError && errorField === "forename" ? "nhsuk-input--error" : ""}`} id="forename" name="forename" type="text" defaultValue={values?.forename} data-testid="forename-input" />
        </div>

        <div className={`nhsuk-form-group ${hasError && errorField === "surname" ? "nhsuk-form-group--error" : ""}`}>
          <label className="nhsuk-label nhsuk-u-font-weight-bold" htmlFor="surname">Surname</label>
          {hasError && errorField === "surname" && (
            <span className="nhsuk-error-message" id="surname-error">
              <span className="nhsuk-u-visually-hidden">Error:</span>{" "}
              {errorMessage}
            </span>
          )}
          <input className={`nhsuk-input ${hasError && errorField === "surname" ? "nhsuk-input--error" : ""}`} id="surname" name="surname" type="text" defaultValue={values?.surname} data-testid="surname-input" />
        </div>

        <div className={`nhsuk-form-group ${hasError && errorField === "dob-day" ? "nhsuk-form-group--error" : ""}`}>
          <fieldset className="nhsuk-fieldset" aria-describedby="dob-hint" role="group">
            <legend className="nhsuk-fieldset__legend nhsuk-label nhsuk-u-font-weight-bold">Date of Birth</legend>
            <div className="nhsuk-hint" id="dob-hint">For example, 15 3 1984</div>
            {hasError && errorField === "dob-day" && (
              <span className="nhsuk-error-message" id="dob-error">
                <span className="nhsuk-u-visually-hidden">Error:</span>{" "}
                {errorMessage}
              </span>
            )}
            <div className="nhsuk-date-input" id="dob">
              <div className="nhsuk-date-input__item">
                <div className="nhsuk-form-group">
                  <label className="nhsuk-label nhsuk-date-input__label" htmlFor="dob-day">Day</label>
                  <input className={`nhsuk-input nhsuk-date-input__input nhsuk-input--width-2 ${hasError && errorField === "dob-day" ? "nhsuk-input--error" : ""}`} id="dob-day" name="dob-day" type="text" inputMode="numeric" maxLength={2} defaultValue={values?.dobDay} data-testid="dob-day-input" />
                </div>
              </div>
              <div className="nhsuk-date-input__item">
                <div className="nhsuk-form-group">
                  <label className="nhsuk-label nhsuk-date-input__label" htmlFor="dob-month">Month</label>
                  <input className={`nhsuk-input nhsuk-date-input__input nhsuk-input--width-2 ${hasError && errorField === "dob-day" ? "nhsuk-input--error" : ""}`} id="dob-month" name="dob-month" type="text" inputMode="numeric" maxLength={2} defaultValue={values?.dobMonth} data-testid="dob-month-input" />
                </div>
              </div>
              <div className="nhsuk-date-input__item">
                <div className="nhsuk-form-group">
                  <label className="nhsuk-label nhsuk-date-input__label" htmlFor="dob-year">Year</label>
                  <input className={`nhsuk-input nhsuk-date-input__input nhsuk-input--width-4 ${hasError && errorField === "dob-day" ? "nhsuk-input--error" : ""}`} id="dob-year" name="dob-year" type="text" inputMode="numeric" maxLength={4} defaultValue={values?.dobYear} data-testid="dob-year-input" />
                </div>
              </div>
            </div>
          </fieldset>
        </div>

        <div className={`nhsuk-form-group ${hasError && errorField === "serviceNowTicketNumber" ? "nhsuk-form-group--error" : ""}`}>
          <label className="nhsuk-label nhsuk-u-font-weight-bold" htmlFor="serviceNowTicketNumber">Service Now Ticket Number</label>
          <div className="nhsuk-hint" id="serviceNowTicketNumber-hint">For example, CS1234567</div>
          {hasError && errorField === "serviceNowTicketNumber" && (
            <span className="nhsuk-error-message" id="serviceNowTicketNumber-error">
              <span className="nhsuk-u-visually-hidden">Error:</span>{" "}
              {errorMessage}
            </span>
          )}
          <input className={`nhsuk-input nhsuk-input--width-10 ${hasError && errorField === "serviceNowTicketNumber" ? "nhsuk-input--error" : ""}`} id="serviceNowTicketNumber" name="serviceNowTicketNumber" type="text" defaultValue={values?.serviceNowTicketNumber} aria-describedby="serviceNowTicketNumber-hint" data-testid="service-now-ticket-input"/>
        </div>

        <button className="nhsuk-button" data-module="nhsuk-button" type="submit" data-testid="submit-button">Submit</button>
      </form>
    </>
  );
}
