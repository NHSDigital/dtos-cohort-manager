import type { Metadata } from "next";
import { auth } from "@/app/lib/auth";
import { getIsCohortManager } from "@/app/lib/access";
import Overview from "@/app/components/overview";
import SignIn from "@/app/components/signIn";
import Unauthorised from "./components/unauthorised";

export async function generateMetadata(): Promise<Metadata> {
  const session = await auth();

  if (session?.user) {
    return {
      title: `Breast screening - ${process.env.SERVICE_NAME} - NHS`,
    };
  }

  return {
    title: `${process.env.SERVICE_NAME} - NHS`,
  };
}

export default async function Home() {
  const serviceName = process.env.SERVICE_NAME;
  const session = await auth();
  const isSignedIn = !!session?.user;
  const isCohortManager = await getIsCohortManager(session);

  if (!isSignedIn) {
    return <SignIn serviceName={serviceName} />;
  }

  if (!isCohortManager) {
    return <Unauthorised />;
  }

  return <Overview />;
}
