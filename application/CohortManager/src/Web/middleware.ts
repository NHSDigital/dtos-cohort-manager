export { auth as middleware } from "@/app/lib/auth";

export const config = {
  matcher: [
    "/account",
    "/exceptions/:path*",
    "/participant-information/:path*",
    "/reports/:path*",
  ],
};
