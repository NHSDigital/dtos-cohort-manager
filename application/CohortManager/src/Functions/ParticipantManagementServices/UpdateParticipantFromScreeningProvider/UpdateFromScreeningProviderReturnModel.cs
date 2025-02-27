namespace NHS.CohortManager.ParticipantManagementServices;

using Model;

/// <summary>
/// Converts a BI & Analytics DTO participant and reference data to the
/// format expected by BI & Analytics
/// </summary>
public class UpdateFromScreeningProviderReturnModel(BiAnalyticsParticipantDto participant, GeneCodeLkp? geneCodeLkp,
                                                    HigherRiskReferralReasonLkp? higherRiskReferralReasonLkp)
{
    public Int64 ScreeningId { get; set; } = participant.ScreeningId;
    public Int64 NHSNumber { get; set; } = participant.NhsNumber;
    public string? ReasonForRemoval { get; set; }
    public DateTime? ReasonForRemovalDate { get; set; }
    public DateOnly? NextTestDueDate { get; set; } = participant.NextTestDueDate;
    public string? NextTestDueDateCalcMethod { get; set; } = participant.NextTestDueDateCalculationMethod;
    public string? ParticipantScreeningStatus { get; set; } = participant.ParticipantScreeningStatus;
    public string? ScreeningCeasedReason { get; set; } = participant.ScreeningCeasedReason;
    public Int16? IsHigherRisk { get; set; } = participant.IsHigherRisk;
    public Int16? IsHigherRiskActive { get; set; } = participant.IsHigherRiskActive;
    public DateOnly? HigherRiskNextTestDueDate { get; set; } = participant.HigherRiskNextTestDueDate;
    public DateOnly? DateIrradiated { get; set; } = participant.DateIrradiated;
    public string? HigherRiskReferralReasonCode { get; set; } = higherRiskReferralReasonLkp?.HigherRiskReferralReasonCode;
    public string? HigherRiskReferralReasonDescription { get; set; } = higherRiskReferralReasonLkp?.HigherRiskReferralReasonCodeDescription;
    public string? GeneCode { get; set; } = geneCodeLkp?.GeneCode;
    public string? GeneCodeDescription { get; set; } = geneCodeLkp?.GeneCodeDescription;
    public DateTime? SrcSysProcessedDateTime { get; set; } = participant.SrcSysProcessedDateTime;
}