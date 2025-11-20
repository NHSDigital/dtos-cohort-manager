const UserFeedback = () => {
  return (
    <div className="app-feedback-section">
      <hr />

      <h2 className="nhsuk-u-padding-top-4">Help us improve</h2>
      <p>Your feedback helps us make our service better.</p>
      <a
        className="nhsuk-action-link"
        href="https://feedback.digital.nhs.uk/jfe/form/SV_3fSsaWEgsDZ2DJA"
        target="_blank"
        rel="noopener noreferrer"
      >
        <svg
          className="nhsuk-icon nhsuk-icon--arrow-right-circle"
          xmlns="http://www.w3.org/2000/svg"
          viewBox="0 0 24 24"
          width="25"
          height="25"
          focusable="false"
          aria-hidden="true"
        >
          <path d="M12 2a10 10 0 0 0-10 9h11.7l-4-4a1 1 0 0 1 1.5-1.4l5.6 5.7a1 1 0 0 1 0 1.4l-5.6 5.7a1 1 0 0 1-1.5 0 1 1 0 0 1 0-1.4l4-4H2A10 10 0 1 0 12 2z" />
        </svg>
        <span className="nhsuk-action-link__text">
          Let us know about your experience of Cohort Manager
        </span>
      </a>
      <p>
        If you need technical support, please continue to use our{' '}
        <a href="/contact-us">contact us</a> page rather than this form.
      </p>
    </div>
  );
};

export default UserFeedback;
