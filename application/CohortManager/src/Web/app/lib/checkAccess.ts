export async function checkAccess(uid: string) {
  const cohortManagerUsers = (process.env.COHORT_MANAGER_USERS || "").split(
    ","
  );
  return cohortManagerUsers.includes(uid);
}
