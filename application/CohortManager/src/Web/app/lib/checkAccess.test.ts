import { checkAccess } from "@/app/lib/checkAccess";

describe("checkAccess", () => {
  it("returns true if the user is a cohort manager", async () => {
    process.env.COHORT_MANAGER_USERS = "123,456";
    const result = await checkAccess("123");
    expect(result).toBe(true);
  });
  it("returns false if the user is not a cohort manager", async () => {
    process.env.COHORT_MANAGER_USERS = "123,456";
    const result = await checkAccess("789");
    expect(result).toBe(false);
  });
});
