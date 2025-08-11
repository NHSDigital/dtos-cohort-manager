import { auth } from "@/app/lib/auth";
import Link from "next/link";

export default async function Unauthorised() {
  const session = await auth();
  return (
    <main className="nhsuk-main-wrapper" id="maincontent" role="main">
      <div className="nhsuk-grid-row">
        <div className="nhsuk-grid-column-two-thirds">
          <h1>You are not authorised to view this page</h1>
          <p>
            <Link href="/contact-us">Contact us</Link> to request access,
            providing:
          </p>
          <ul>
            <li>your name</li>
            <li>your user ID ({session?.user?.uid})</li>
            <li>details of the page you are trying to access</li>
          </ul>
        </div>
      </div>
    </main>
  );
}
