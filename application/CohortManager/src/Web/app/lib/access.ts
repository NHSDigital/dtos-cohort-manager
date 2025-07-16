import { checkAccess } from "./checkAccess";
import { Session } from "next-auth";

export async function getIsCohortManager(
  session: Session | null
): Promise<boolean> {
  if (!session?.user) return false;
  return await checkAccess(session.user.workgroups_codes || []);
}
