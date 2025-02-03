export { auth as middleware } from "@/app/lib/auth";

export const config = {
  matcher: [
    "/account",
    "/exceptions-summary",
    "/participant-information/:path*",
  ],
};
