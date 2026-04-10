import type { Metadata } from "next";
import { auth } from "@/app/lib/auth";
import Breadcrumb from "@/app/components/breadcrumb";

export const metadata: Metadata = {
  title: `Account and settings - ${process.env.SERVICE_NAME} - NHS`,
};

export default async function Page() {
  const breadcrumbItems = [{ label: "Home", url: "/" }];
  const session = await auth();
  const isDevelopment = process.env.APP_ENV === "development";

  return (
    <>
      <Breadcrumb items={breadcrumbItems} />
      <main className="nhsuk-main-wrapper" id="maincontent" role="main">
        <div className="nhsuk-grid-row">
          <div className="nhsuk-grid-column-two-thirds">
            <h1>Account and settings</h1>
            <dl className="nhsuk-summary-list">
              <div className="nhsuk-summary-list__row">
                <dt className="nhsuk-summary-list__key">UID</dt>
                <dd className="nhsuk-summary-list__value">
                  {session?.user?.uid}
                </dd>
              </div>
              <div className="nhsuk-summary-list__row">
                <dt className="nhsuk-summary-list__key">Name</dt>
                <dd className="nhsuk-summary-list__value">
                  {session?.user?.firstName} {session?.user?.lastName}
                </dd>
              </div>
              <div className="nhsuk-summary-list__row">
                <dt className="nhsuk-summary-list__key">Workgroups</dt>
                <dd className="nhsuk-summary-list__value">
                  <ul>
                    {(session?.user?.workgroups || []).map((group: string) => (
                      <li key={group}>{group}</li>
                    ))}
                  </ul>
                </dd>
              </div>
              <div className="nhsuk-summary-list__row">
                <dt className="nhsuk-summary-list__key">Workgroups codes</dt>
                <dd className="nhsuk-summary-list__value">
                  <ul>
                    {(session?.user?.workgroups_codes || []).map(
                      (group: string) => (
                        <li key={group}>{group}</li>
                      )
                    )}
                  </ul>
                </dd>
              </div>
              {isDevelopment ? (
                <>
                  <div className="nhsuk-summary-list__row">
                    <dt className="nhsuk-summary-list__key">
                      Development ID token
                    </dt>
                    <dd className="nhsuk-summary-list__value">
                      <p>
                        Set AUTH_CIS2_DEV_ID_TOKEN or AUTH_CIS2_DEV_JWT_TOKEN
                        in your local env to supply a JWT when using the
                        development credentials sign-in.
                      </p>
                      <pre>{session?.idToken || "No ID token available"}</pre>
                    </dd>
                  </div>
                  <div className="nhsuk-summary-list__row">
                    <dt className="nhsuk-summary-list__key">
                      Development access token
                    </dt>
                    <dd className="nhsuk-summary-list__value">
                      <pre>
                        {session?.accessToken || "No access token available"}
                      </pre>
                    </dd>
                  </div>
                </>
              ) : null}
            </dl>
          </div>
        </div>
      </main>
    </>
  );
}
