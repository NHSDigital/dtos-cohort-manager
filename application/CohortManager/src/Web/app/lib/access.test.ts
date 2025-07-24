import { canAccessCohortManager } from "./access";
import { Session } from "next-auth";

describe("canAccessCohortManager", () => {
  it("returns true if session user has matching workgroup code", async () => {
    process.env.COHORT_MANAGER_RBAC_CODE = "CODE1,CODE2";
    const session: Session = {
      user: { uid: "test", workgroups_codes: ["CODE2", "OTHER"] },
      expires: "",
    };
    const result = await canAccessCohortManager(session);
    expect(result).toBe(true);
  });

  it("returns false if session user does not have matching workgroup code", async () => {
    process.env.COHORT_MANAGER_RBAC_CODE = "CODE1,CODE2";
    const session: Session = {
      user: { uid: "test", workgroups_codes: ["OTHER", "ANOTHER"] },
      expires: "",
    };
    const result = await canAccessCohortManager(session);
    expect(result).toBe(false);
  });

  it("returns false if session is null", async () => {
    process.env.COHORT_MANAGER_RBAC_CODE = "CODE1";
    const result = await canAccessCohortManager(null);
    expect(result).toBe(false);
  });

  it("returns false if session.user is missing", async () => {
    process.env.COHORT_MANAGER_RBAC_CODE = "CODE1";
    const session: Session = {
      user: undefined,
      expires: "",
    };
    const result = await canAccessCohortManager(session);
    expect(result).toBe(false);
  });
});
