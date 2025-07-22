import type { Metadata } from "next";
import Breadcrumb from "@/app/components/breadcrumb";

export const metadata: Metadata = {
  title: `Accessibility statement - ${process.env.SERVICE_NAME} - NHS`,
};

export default async function Page() {
  const breadcrumbItems = [{ label: "Home", url: "/" }];

  return (
    <>
      <Breadcrumb items={breadcrumbItems} />
      <main className="nhsuk-main-wrapper" id="maincontent" role="main">
        <div className="nhsuk-grid-row">
          <div className="nhsuk-grid-column-two-thirds">
            <h1>Accessibility statement</h1>
            <p>
              This accessibility statement applies to the Cohort Manager
              service.
            </p>
            <p>
              We want as many people as possible to be able to use this website.
              This means you should be able to:
            </p>
            <ul>
              <li>
                change colours, contrast levels and fonts using browser
                functionality
              </li>
              <li>
                zoom in up to 400 per cent without the text spilling off the
                screen
              </li>
              <li>navigate most of the website using just a keyboard</li>
              <li>
                navigate most of the website using speech recognition software
              </li>
              <li>
                interact with most of the website using a screen reader
                (including recent versions of JAWS, NVDA and VoiceOver)
              </li>
            </ul>
            <p>
              We also try to make the website text as simple as possible to
              understand.
            </p>
            <p>
              If you have a disability,{" "}
              <a href="https://mcmw.abilitynet.org.uk/">
                search AbilityNet for &quot;how to&quot; guides
              </a>{" "}
              to make your device easier to use.
            </p>
            <h2>Feedback and contact information</h2>
            <p>
              If you have feedback, or need information on this website in a
              different format, contact{" "}
              <a href="mailto:england.digitalscreening@nhs.net">
                england.digitalscreening@nhs.net
              </a>
            </p>
            <h2>Reporting accessibility problems with this website</h2>
            <p>
              We&apos;re always looking to improve the accessibility of this
              website. If you find any problems not listed on this page or think
              we&apos;re not meeting accessibility requirements, please contact{" "}
              <a href="mailto:england.digitalscreening@nhs.net">
                england.digitalscreening@nhs.net
              </a>
              . This helps us improve.
            </p>
            <h2>Enforcement procedure</h2>
            <p>
              If you contact us with a complaint and you are not happy with our
              response,{" "}
              <a href="https://www.equalityadvisoryservice.com/">
                contact the Equality Advisory and Support Service (EASS)
              </a>
              .
            </p>
            <p>
              The Equality and Human Rights Commission (EHRC) is responsible for
              enforcing the{" "}
              <a href="https://www.legislation.gov.uk/uksi/2018/952/contents/made">
                Public Sector Bodies (Websites and Mobile Applications) (No. 2)
                Accessibility Regulations 2018 on legislation.gov.uk
              </a>{" "}
              (the &quot;accessibility regulations&quot;).
            </p>
            <h2>
              Technical information about this website&apos;s accessibility
            </h2>
            <p>
              This service is compliant with the{" "}
              <a href="http://www.w3.org/TR/WCAG21/">
                Web Content Accessibility Guidelines (WCAG) version 2.1 AA
                standard
              </a>
              .
            </p>
            <h2>Preparation of this accessibility statement</h2>
            <p>This statement was prepared on 2 December 2024.</p>
            <p>
              This website&apos;s accessibility will be reviewed on a regular
              and continuous basis.
            </p>
          </div>
        </div>
      </main>
    </>
  );
}
