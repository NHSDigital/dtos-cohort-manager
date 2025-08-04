import { checkAccess } from "@/app/lib/checkAccess";

describe("checkAccess", () => {
  it("returns true if any workgroup code matches", async () => {
    process.env.COHORT_MANAGER_RBAC_CODE = "CODE1,CODE2";
    const result = await checkAccess(["CODE2", "OTHER"]);
    expect(result).toBe(true);
  });

  it("returns false if no workgroup code matches", async () => {
    process.env.COHORT_MANAGER_RBAC_CODE = "CODE1,CODE2";
    const result = await checkAccess(["OTHER", "ANOTHER"]);
    expect(result).toBe(false);
  });

  it("handles spaces and empty codes", async () => {
    process.env.COHORT_MANAGER_RBAC_CODE = " CODE1 , ,CODE2 ";
    const result = await checkAccess(["CODE1"]);
    expect(result).toBe(true);
  });
});
