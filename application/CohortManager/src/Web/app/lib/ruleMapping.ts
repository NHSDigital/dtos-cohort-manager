import { RuleMapping } from "@/app/types";

export const ruleIdMappings: Record<number, RuleMapping> = {
  3: {
    ruleDescription:
      "It's not possible to have both a current GP practice code and a current reason for removal (RfR), or neither.",
    moreDetails:
      "Either: enter a GP practice code, or enter a reason for removal. Information must only be entered for one of these fields. Both fields cannot be empty.",
    reportingId: "CMR03",
  },
  8: {
    ruleDescription:
      "Record from CaaS is either missing from or is not recognised by Cohort Manager.",
    moreDetails: "Raise with Cohorting as a Service (CaaS).",
    reportingId: "CMR4",
  },
  10: {
    ruleDescription:
      "Update received for NHS number with reason for removal of ‘DEA’.",
    moreDetails: "An update has been received for a record marked as deceased.",
    reportingId: "CMR36",
  },
  17: {
    ruleDescription:
      "Date of birth is either missing, in the wrong format, or is in the future.",
    moreDetails:
      "Enter the date of birth in the correct format. The date must not be in the future. ",
    reportingId: "CMR14",
  },
  18: {
    ruleDescription:
      "Date of death is in the wrong format or is in the future.",
    moreDetails:
      "Enter the date of death in the correct format. The date cannot be in the future.",
    reportingId: "CMR22",
  },
  21: {
    ruleDescription:
      "The 'Superseded by NHS number' field has been populated with an NHS number by NBO.",
    moreDetails: "",
    reportingId: "CMR33",
  },
  22: {
    ruleDescription:
      "Amendment received for an NHS number that is not in this cohort.",
    moreDetails:
      "Raise with Cohorting as a Service (CaaS). An amendment cannot be applied as the record has not yet been added to the cohort. There may have been a delay in adding the new record to Cohort Manager.  ",
    reportingId: "CMR39",
  },
  30: {
    ruleDescription: "Postcode is in the wrong format.",
    moreDetails: "Enter a valid UK postcode.",
    reportingId: "CMR12",
  },
  35: {
    ruleDescription:
      "Possible confusion. At least 2 of the following have changed: family name, gender or date of birth",
    moreDetails:
      "Verify that the changes are correct, and that the NHS number is not involved in a confusion case.",
    reportingId: "CMR38",
  },
  36: {
    ruleDescription: "GP practice code does not exist.",
    moreDetails: "Raise with Breast Screening Select (BSS).",
    reportingId: "CMR45",
  },
  39: {
    ruleDescription: "Missing surname.",
    moreDetails: "Enter the patient's surname.",
    reportingId: "CMR15",
  },
  40: {
    ruleDescription: "Missing forename.",
    moreDetails: "Enter the patient's forename.",
    reportingId: "CMR16",
  },
  54: {
    ruleDescription:
      "A dummy GP practice code could not be generated. An English postcode must be entered.",
    moreDetails:
      "A dummy GP practice code is used by breast screening offices when a participant’s actual GP practice information is unknown. The relevant dummy GP practice code will be generated when a postcode is added to the record.",
    reportingId: "CMR34",
  },
  66: {
    ruleDescription:
      "NHS number has been marked as formally dead, but the reason for removal (RfR) is not ‘DEA’.",
    moreDetails:
      "To update a record with a formal death status, ensure that the reason for removal field contains a compatible death-related value.",
    reportingId: "CMR20",
  },
  69: {
    ruleDescription: "NHS number’s invalid flag is set to true.",
    moreDetails:
      "An update has been received for the record with the NHS Number now flagged as invalid.",
    reportingId: "CMR29",
  },
  71: {
    ruleDescription: "Address is blank (postcode may be blank too).",
    moreDetails: "Enter the patient's full address and postcode.",
    reportingId: "CMR17",
  },
};

export function getRuleMapping(
  ruleId: number,
  fallbackDescription?: string
): RuleMapping {
  return (
    ruleIdMappings[ruleId] || {
      ruleDescription: fallbackDescription,
      moreDetails: fallbackDescription,
      reportingId: undefined,
    }
  );
}
