import type { Metadata } from "next";
import { auth } from "@/app/lib/auth";
import { checkAccess } from "@/app/lib/checkAccess";
import Overview from "@/app/components/overview";
import SignIn from "@/app/components/signIn";
import Unauthorised from "./components/unauthorised";

export async function generateMetadata(): Promise<Metadata> {
  const session = await auth();

  if (session?.user) {
    return {
      title: `Overview - ${process.env.SERVICE_NAME}`,
    };
  }

  return {
    title: `Log in with your Care Identity account - ${process.env.SERVICE_NAME}`,
  };
}

export default async function Home() {
  const serviceName = process.env.SERVICE_NAME;
  const session = await auth();
  const isSignedIn = !!session?.user;
  const isCohortManager = session?.user
    ? await checkAccess(session.user.uid)
    : false;

  if (!isSignedIn) {
    return <SignIn serviceName={serviceName} />;
  }

  if (!isCohortManager) {
    return <Unauthorised />;
  }

  return <Overview />;
}
