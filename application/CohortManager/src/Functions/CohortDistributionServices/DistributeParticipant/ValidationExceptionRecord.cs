namespace NHS.CohortManager.CohortDistributionServices;

using Model;

/// <summary>
/// Wrapper needed for handling validation exceptions
/// (you cannot pass in mutiple params to durable function activites)
/// </summary>
public class ValidationExceptionRecord
{
    public ValidationRecord ValidationRecord { get; set; }
    public List<ValidationRuleResult> ValidationExceptions { get; set; }

    public ValidationExceptionRecord(ValidationRecord validationRecord, List<ValidationRuleResult> validationExceptions)
    {
        ValidationRecord = validationRecord;
        ValidationExceptions = validationExceptions;
    }
}