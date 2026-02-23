export { auth as proxy } from "@/app/lib/auth";

export const config = {
  matcher: [
    "/account",
    "/exceptions/:path*",
    "/participant-information/:path*",
    "/reports/:path*",
  ],
};
