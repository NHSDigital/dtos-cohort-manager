import { getRuleMapping, ruleIdMappings } from "@/app/lib/ruleMapping";

describe("ruleMapping", () => {
  describe("ruleIdMappings", () => {
    it("should contain all expected rule IDs", () => {
      const expectedRuleIds = [
        3, 8, 17, 18, 30, 35, 39, 40, 54, 66, 71
      ];
      const actualRuleIds = Object.keys(ruleIdMappings).map(Number);

      expectedRuleIds.forEach((ruleId) => {
        expect(actualRuleIds).toContain(ruleId);
      });
    });

    it("should have valid structure for each rule mapping", () => {
      Object.entries(ruleIdMappings).forEach(([, mapping]) => {
        expect(mapping).toHaveProperty("ruleDescription");
        expect(mapping).toHaveProperty("moreDetails");
        expect(mapping).toHaveProperty("reportingId");
        expect(typeof mapping.ruleDescription).toBe("string");
        expect(mapping.ruleDescription.length).toBeGreaterThan(0);
        expect(["string", "undefined"]).toContain(typeof mapping.reportingId);
        if (mapping.reportingId) {
          expect(mapping.reportingId.length).toBeGreaterThan(0);
        }
      });
    });

    it("should have unique reporting IDs", () => {
      const reportingIds = Object.values(ruleIdMappings)
        .map((mapping) => mapping.reportingId)
        .filter((id) => id !== undefined);
      const uniqueReportingIds = new Set(reportingIds);
      expect(reportingIds.length).toBe(uniqueReportingIds.size);
    });
  });

  describe("getRuleMapping", () => {
    describe("when rule ID exists in mappings", () => {
      it("should return the correct mapping for rule ID 3", () => {
        const result = getRuleMapping(3);
        expect(result).toEqual({
          ruleDescription:
            "It's not possible to have both a current GP practice code and a current reason for removal (RfR), or neither.",
          moreDetails:
            "Either: enter a GP practice code, or enter a reason for removal. Information must only be entered for one of these fields. Both fields cannot be empty.",
          reportingId: "CMR13",
        });
      });

      it("should return the correct mapping for rule ID 17", () => {
        const result = getRuleMapping(17);
        expect(result).toEqual({
          ruleDescription:
            "Date of birth is either missing, in the wrong format, or is in the future.",
          moreDetails:
            "Enter the date of birth in the correct format. The date must not be in the future. ",
          reportingId: "CMR14",
        });
      });

      it("should return the correct mapping for rule ID 71", () => {
        const result = getRuleMapping(71);
        expect(result).toEqual({
          ruleDescription: "Address is blank (postcode may be blank too).",
          moreDetails: "Enter the patient's full address and postcode.",
          reportingId: "CMR17",
        });
      });

      it("should not use fallback description when rule exists", () => {
        const fallbackDescription = "This should not be used";
        const result = getRuleMapping(3, fallbackDescription);
        expect(result.ruleDescription).not.toBe(fallbackDescription);
        expect(result.ruleDescription).toBe(
          "It's not possible to have both a current GP practice code and a current reason for removal (RfR), or neither."
        );
      });
    });

    describe("when rule ID does not exist in mappings", () => {
      it("should return fallback description when provided", () => {
        const fallbackDescription = "Custom API description";
        const result = getRuleMapping(999, fallbackDescription);

        expect(result).toEqual({
          ruleDescription: fallbackDescription,
          moreDetails: fallbackDescription,
          reportingId: undefined,
        });
      });

      it("should return undefined for all fields when no fallback provided", () => {
        const result = getRuleMapping(999);

        expect(result).toEqual({
          ruleDescription: undefined,
          moreDetails: undefined,
          reportingId: undefined,
        });
      });

      it("should handle empty string as fallback description", () => {
        const result = getRuleMapping(888, "");

        expect(result).toEqual({
          ruleDescription: "",
          moreDetails: "",
          reportingId: undefined,
        });
      });

      it("should handle undefined as fallback description", () => {
        const result = getRuleMapping(777, undefined);

        expect(result).toEqual({
          ruleDescription: undefined,
          moreDetails: undefined,
          reportingId: undefined,
        });
      });
    });

    describe("return type validation", () => {
      it("should return RuleMapping interface structure", () => {
        const result = getRuleMapping(3);

        expect(result).toHaveProperty("ruleDescription");
        expect(result).toHaveProperty("moreDetails");
        expect(result).toHaveProperty("reportingId");
      });

      it("should return RuleMapping interface structure for unmapped rules", () => {
        const result = getRuleMapping(999, "Test description");

        expect(result).toHaveProperty("ruleDescription");
        expect(result).toHaveProperty("moreDetails");
        expect(result).toHaveProperty("reportingId");
      });
    });
  });

  describe("specific rule validations", () => {
    it("should have correct CMR reporting IDs format", () => {
      Object.values(ruleIdMappings)
        .filter((mapping) => mapping.reportingId !== undefined)
        .forEach((mapping) => {
          expect(mapping.reportingId).toMatch(/^CMR\d+$/);
        });
    });

    it("should have non-empty rule descriptions", () => {
      Object.values(ruleIdMappings).forEach((mapping) => {
        expect(mapping.ruleDescription.trim()).not.toBe("");
      });
    });
  });
});
