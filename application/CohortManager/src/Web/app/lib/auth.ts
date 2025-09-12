import NextAuth, { Profile, Session, User } from "next-auth";
import Credentials from "next-auth/providers/credentials";
import { JWT } from "next-auth/jwt";
import { jwtDecode } from "jwt-decode";
import { OAuthConfig } from "next-auth/providers";
import { DecodedCIS2Token } from "@/app/types/auth";

const NHS_CIS2: OAuthConfig<Profile> = {
  id: "nhs-cis2",
  name: "NHS CIS2 Authentication",
  type: "oidc",
  issuer: `${process.env.AUTH_CIS2_ISSUER_URL}/openam/oauth2/realms/root/realms/NHSIdentity/realms/Healthcare`,
  wellKnown: `${process.env.AUTH_CIS2_ISSUER_URL}/openam/oauth2/realms/root/realms/NHSIdentity/realms/Healthcare/.well-known/openid-configuration`,
  clientId: process.env.AUTH_CIS2_CLIENT_ID,
  clientSecret: process.env.AUTH_CIS2_CLIENT_SECRET,
  authorization: {
    params: {
      acr_values: "AAL2_OR_AAL3_ANY",
      scope: "openid profile nationalrbacaccess",
      response_type: "code",
      max_age: 240, // 4 minutes [Required by CIS2]
    },
  },
  client: {
    token_endpoint_auth_method: "client_secret_post",
  },
  idToken: false,
  checks: ["state"],
};

export const { handlers, auth, signIn, signOut } = NextAuth({
  providers: [
    NHS_CIS2,
    ...(process.env.APP_ENV === "development"
      ? [
          Credentials({
            credentials: {
              email: {},
              password: {},
            },
            authorize: async () => {
              const user: User = {
                uid: "testuid",
                firstName: "Test",
                lastName: "User",
              };
              return user;
            },
          }),
        ]
      : []),
  ],
  session: {
    strategy: "jwt",
    maxAge: 900, // 15 minutes [Required by CIS2]
  },
  callbacks: {
    authorized: async ({ auth }) => {
      // Logged in users are authenticated, otherwise redirect to login page
      return !!auth;
    },
    async signIn({ account }) {
      // Handle test accounts in development
      if (
        process.env.APP_ENV === "development" &&
        account?.provider === "credentials"
      ) {
        return true;
      }

      if (!account || typeof account.id_token !== "string") {
        return false;
      }

      const decodedToken: DecodedCIS2Token = jwtDecode(account.id_token);
      const AUTH_CIS2_ISSUER_URL = `${process.env.AUTH_CIS2_ISSUER_URL}/openam/oauth2/realms/root/realms/NHSIdentity/realms/Healthcare`;
      const AUTH_CIS2_CLIENT_ID = process.env.AUTH_CIS2_CLIENT_ID;

      const { iss, aud, idassurancelevel, authentication_assurance_level } =
        decodedToken;

      const isValidToken =
        iss === AUTH_CIS2_ISSUER_URL &&
        aud === AUTH_CIS2_CLIENT_ID &&
        idassurancelevel >= "2" &&
        authentication_assurance_level >= "2";

      return isValidToken;
    },
    async jwt({ account, token, profile }) {
      if (account?.access_token) {
        try {
          const response = await fetch(
            `${process.env.AUTH_CIS2_ISSUER_URL}/openam/oauth2/realms/root/realms/NHSIdentity/realms/Healthcare/userinfo`,
            {
              method: "GET",
              headers: {
                Authorization: `Bearer ${account.access_token}`,
              },
            }
          );
          if (!response.ok) {
            throw new Error("Failed to call the userinfo endpoint from CIS2");
          }
          const userInfo: Profile = await response.json();
          token.profile = userInfo;
        } catch (error) {
          console.error("Error fetching user info:", error);
          return token;
        }
      }

      // Handle test accounts in development
      if (
        process.env.APP_ENV === "development" &&
        account?.provider === "credentials"
      ) {
        Object.assign(token, {
          uid: "testuid",
          firstName: "Test",
          lastName: "User",
          sub: "1234",
          sid: "5678",
          workgroups: ["Test Workgroup"],
          workgroups_codes: ["000000000000"],
        });
      }

      if (profile) {
        const {
          uid,
          given_name: firstName,
          family_name: lastName,
          sub,
          sid,
          nhsid_nrbac_roles,
        } = profile;

        const workgroups = (nhsid_nrbac_roles as Array<unknown>).flatMap(
          (role) => (role as { workgroups?: unknown[] }).workgroups || []
        );

        const workgroups_codes = (nhsid_nrbac_roles as Array<unknown>).flatMap(
          (role: unknown) =>
            (role as { workgroups_codes?: unknown[] }).workgroups_codes || []
        );

        Object.assign(token, {
          uid,
          firstName,
          lastName,
          sub: sub ?? undefined,
          sid: sid ?? undefined,
          workgroups,
          workgroups_codes,
        });
      }
      return token;
    },
    async session({ session, token }: { session: Session; token: JWT }) {
      if (session.user) {
        const {
          uid,
          firstName,
          lastName,
          sub,
          sid,
          workgroups,
          workgroups_codes,
        } = token;

        Object.assign(session.user, {
          uid,
          firstName,
          lastName,
          sub,
          sid,
          workgroups,
          workgroups_codes,
        });
      }
      return session;
    },
  },
  pages: {
    signIn: "/",
    error: "/error",
  },
  events: {
    async session({ session }) {
      const maxAge = 900; // 15 minutes [Required by CIS2]
      const now = Math.floor(Date.now() / 1000);
      session.expires = new Date((now + maxAge) * 1000).toISOString();
    },
  },
});
