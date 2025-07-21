import type { Metadata } from "next";
import AuthError from "@/app/components/authError";

export const metadata: Metadata = {
  title: `Something went wrong - ${process.env.SERVICE_NAME} - NHS`,
};

export default function AuthErrorPage() {
  return (
    <main className="nhsuk-main-wrapper" id="maincontent" role="main">
      <div className="nhsuk-grid-row">
        <div className="nhsuk-grid-column-two-thirds">
          <AuthError />
        </div>
      </div>
    </main>
  );
}
