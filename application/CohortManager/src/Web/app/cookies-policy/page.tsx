import type { Metadata } from "next";
import Breadcrumb from "@/app/components/breadcrumb";

export const metadata: Metadata = {
  title: `Cookies on ${process.env.SERVICE_NAME}  - ${process.env.SERVICE_NAME} - NHS`,
};

export default async function Page() {
  const breadcrumbItems = [{ label: "Home", url: "/" }];

  return (
    <>
      <Breadcrumb items={breadcrumbItems} />
      <main className="nhsuk-main-wrapper" id="maincontent" role="main">
        <div className="nhsuk-grid-row">
          <div className="nhsuk-grid-column-two-thirds">
            <h1>Cookies on Cohort Manager</h1>
            <h2>What are cookies?</h2>
            <p>
              Cookies are files saved on your phone, tablet or computer when you
              visit a website.
            </p>
            <p>
              They store information about how you use the website, such as the
              pages you visit.
            </p>
            <p>
              Cookies are not viruses or computer programs. They are very small
              so do not take up much space.
            </p>
            <h2>How we use cookies</h2>
            <p>
              We only use cookies that are needed to make our website work, for
              example by keeping it secure.
            </p>
            <p>We do not use any other type of cookies.</p>
            <h2>Cookies needed for the website to work</h2>
            <table className="nhsuk-table nhsuk-u-margin-top-0">
              <caption className="nhsuk-table__caption">
                List of cookies that are needed to make the website work
              </caption>
              <thead className="nhsuk-table__head">
                <tr className="nhsuk-table__row">
                  <th className="nhsuk-table__header" scope="col">
                    Name
                  </th>
                  <th className="nhsuk-table__header" scope="col">
                    Purpose
                  </th>
                  <th className="nhsuk-table__header" scope="col">
                    Expires
                  </th>
                </tr>
              </thead>
              <tbody className="nhsuk-table__body">
                <tr className="nhsuk-table__row">
                  <td className="nhsuk-table__cell">
                    __Host-authjs.csrf-token
                  </td>
                  <td className="nhsuk-table__cell">
                    Prevent cross-site request forgery (CSRF) and ensure the
                    security of the website and users.
                  </td>
                  <td className="nhsuk-table__cell">Session</td>
                </tr>
                <tr className="nhsuk-table__row">
                  <td className="nhsuk-table__cell">
                    __Secure-authjs.callback-url
                  </td>
                  <td className="nhsuk-table__cell">
                    Callback URL (also know as redirect URL) used for the
                    authentication to specify where the user should be
                    redirected to once authenticated.
                  </td>
                  <td className="nhsuk-table__cell">Session</td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      </main>
    </>
  );
}
