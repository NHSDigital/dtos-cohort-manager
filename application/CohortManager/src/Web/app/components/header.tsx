import Link from "next/link";
import { auth, signOut } from "@/app/lib/auth";
import styles from "@/app/styles/components/header.module.scss";

interface HeaderProps {
  readonly serviceName?: string;
}

export default async function Header({
  serviceName = process.env.SERVICE_NAME,
}: Readonly<HeaderProps>) {
  const session = await auth();

  return (
    <header className={styles["nhsuk-header"]} role="banner">
      <div className={styles["nhsuk-header__container"]}>
        <div className={styles["nhsuk-header__service"]}>
          <Link
            className={styles["nhsuk-header__service-logo"]}
            href="/"
            aria-label="Manage your screening homepage"
          >
            <svg
              className={styles["nhsuk-logo"]}
              xmlns="http://www.w3.org/2000/svg"
              viewBox="0 0 200 80"
              height="40"
              width="100"
            >
              <path
                className={styles["nhsuk-logo__path"]}
                fill="currentcolor"
                d="M200 0v80H0V0h200Zm-27.5 5.5c-14.5 0-29 5-29 22 0 10.2 7.7 13.5 14.7 16.3l.7.3c5.4 2 10.1 3.9 10.1 8.4 0 6.5-8.5 7.5-14 7.5s-12.5-1.5-16-3.5L135 70c5.5 2 13.5 3.5 20 3.5 15.5 0 32-4.5 32-22.5 0-19.5-25.5-16.5-25.5-25.5 0-5.5 5.5-6.5 12.5-6.5a35 35 0 0 1 14.5 3l4-13.5c-4.5-2-12-3-20-3Zm-131 2h-22l-14 65H22l9-45h.5l13.5 45h21.5l14-65H64l-9 45h-.5l-13-45Zm63 0h-18l-13 65h17l6-28H117l-5.5 28H129l13.5-65H125L119.5 32h-20l5-24.5Z"
              />
            </svg>
            <span className={styles["nhsuk-header__service-name"]}>
              {serviceName}
            </span>
          </Link>
        </div>

        {session?.user && (
          <nav className={styles["nhsuk-header__account"]} aria-label="Account">
            <ul className={styles["nhsuk-header__account-list"]}>
              <li className={styles["nhsuk-header__account-item"]}>
                <svg
                  className={`${styles["nhsuk-icon"]} ${styles["nhsuk-icon__user"]}`}
                  xmlns="http://www.w3.org/2000/svg"
                  width="20"
                  height="20"
                  viewBox="0 0 16 16"
                  aria-hidden="true"
                  focusable="false"
                >
                  <path d="M8 0c4.4 0 8 3.6 8 8s-3.6 8-8 8-8-3.6-8-8 3.6-8 8-8Zm0 1a7 7 0 1 0 0 14A7 7 0 0 0 8 1Zm-1.5 9h3a2.5 2.5 0 0 1 2.5 2.5V14a1 1 0 0 1-1 1H5a1 1 0 0 1-1-1v-1.5A2.5 2.5 0 0 1 6.5 10ZM8 9C6.368 9 5 7.684 5 6s1.316-3 3-3c1.632 0 3 1.316 3 3S9.632 9 8 9" />
                </svg>
                {session.user.firstName} {session.user.lastName}
              </li>
              <li className={styles["nhsuk-header__account-item"]}>
                <a
                  href="/account"
                  className={styles["nhsuk-header__account-link"]}
                >
                  Account and settings
                </a>
              </li>
              <li className={styles["nhsuk-header__account-item"]}>
                <form
                  className={styles["nhsuk-header__account-form"]}
                  action={async () => {
                    "use server";
                    await signOut({ redirectTo: "/" });
                  }}
                  method="post"
                >
                  <button className={styles["nhsuk-header__account-button"]}>
                    Log out
                  </button>
                </form>
              </li>
            </ul>
          </nav>
        )}
      </div>
    </header>
  );
}
