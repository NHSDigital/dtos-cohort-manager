export async function checkAccess(workgroups_codes: string[]) {
  const rbacCodes = (process.env.COHORT_MANAGER_RBAC_CODE || "")
    .split(",")
    .map((code) => code.trim())
    .filter((code) => code.length > 0);
  return rbacCodes.some((code) => workgroups_codes.includes(code));
}
