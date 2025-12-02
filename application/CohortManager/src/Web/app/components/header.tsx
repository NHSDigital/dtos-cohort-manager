import Link from "next/link";
import { auth, signOut } from "@/app/lib/auth";
import { SearchNhsNumber } from "./search-nhs-number";
import { ConditionalHeaderSearch } from "./conditionalHeaderSearch";

interface HeaderProps {
  readonly serviceName?: string;
}

export default async function Header({
  serviceName = process.env.SERVICE_NAME,
}: Readonly<HeaderProps>) {
  const session = await auth();

  return (
    <header className="nhsuk-header" role="banner">
      <div className="nhsuk-header__container nhsuk-width-container">
        <div className="nhsuk-header__content">
          <div className="nhsuk-header__service">
            <Link
              className="nhsuk-header__service-logo"
              href="/"
              aria-label="Cohort manager overview page"
            >
              <svg
                className="nhsuk-header__logo"
                xmlns="http://www.w3.org/2000/svg"
                viewBox="0 0 200 80"
                height="40"
                width="100"
                focusable="false"
                role="img"
                aria-hidden="true"
              >
                <title>NHS</title>
                <path
                  fill="currentcolor"
                  d="M200 0v80H0V0h200Zm-27.5 5.5c-14.5 0-29 5-29 22 0 10.2 7.7 13.5 14.7 16.3l.7.3c5.4 2 10.1 3.9 10.1 8.4 0 6.5-8.5 7.5-14 7.5s-12.5-1.5-16-3.5L135 70c5.5 2 13.5 3.5 20 3.5 15.5 0 32-4.5 32-22.5 0-19.5-25.5-16.5-25.5-25.5 0-5.5 5.5-6.5 12.5-6.5a35 35 0 0 1 14.5 3l4-13.5c-4.5-2-12-3-20-3Zm-131 2h-22l-14 65H22l9-45h.5l13.5 45h21.5l14-65H64l-9 45h-.5l-13-45Zm63 0h-18l-13 65h17l6-28H117l-5.5 28H129l13.5-65H125L119.5 32h-20l5-24.5Z"
                ></path>
              </svg>
              <span className="nhsuk-header__service-name">{serviceName}</span>
            </Link>
          </div>

          {session?.user && (
            <div className="nhsuk-header__search">
              <ConditionalHeaderSearch>
                <SearchNhsNumber />
              </ConditionalHeaderSearch>
            </div>
          )}

          {session?.user && (
            <nav
              className="nhsuk-header__account"
              aria-label="Account"
              data-testid="header-account-navigation"
            >
              <ul className="nhsuk-header__account-list">
                <li className="nhsuk-header__account-item">
                  <svg
                    className="nhsuk-icon nhsuk-icon__user"
                    xmlns="http://www.w3.org/2000/svg"
                    viewBox="0 0 24 24"
                    aria-hidden="true"
                    focusable="false"
                  >
                    <path d="M12 1a11 11 0 1 1 0 22 11 11 0 0 1 0-22Zm0 2a9 9 0 0 0-5 16.5V18a4 4 0 0 1 4-4h2a4 4 0 0 1 4 4v1.5A9 9 0 0 0 12 3Zm0 3a3.5 3.5 0 1 1-3.5 3.5A3.4 3.4 0 0 1 12 6Z"></path>
                  </svg>
                  {session.user.firstName} {session.user.lastName}
                </li>
                <li className="nhsuk-header__account-item">
                  <a href="/account" className="nhsuk-header__account-link">
                    Account and settings
                  </a>
                </li>
                <li className="nhsuk-header__account-item">
                  <form
                    className="nhsuk-header__account-form"
                    action={async () => {
                      "use server";
                      await signOut({ redirectTo: "/" });
                    }}
                  >
                    <button className="nhsuk-header__account-button">
                      Log out
                    </button>
                  </form>
                </li>
              </ul>
            </nav>
          )}
        </div>
      </div>
    </header>
  );
}
