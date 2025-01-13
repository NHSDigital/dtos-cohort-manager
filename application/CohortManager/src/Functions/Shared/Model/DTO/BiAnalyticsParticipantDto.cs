namespace Model;


/// <summary>
/// Participant model used by the BI & Analytics product
/// </summary>
public class BiAnalyticsParticipantDto
{
    public long NhsNumber {get; set;}
    public long ScreeningId {get; set;}
    public DateOnly? NextTestDueDate {get; set;}
    public string? NextTestDueDateCalculationMethod {get; set;}
    public string? ParticipantScreeningStatus {get; set;}
    public string? ScreeningCeasedReason {get; set;}
    public short? IsHigherRisk {get; set;}
    public short? IsHigherRiskActive {get; set;}
    public DateTime SrcSysProcessedDateTime {get; set;}
    public DateOnly? HigherRiskNextTestDueDate {get; set;}
    public string? HigherRiskReferralReasonCode {get; set;}
    public DateOnly? DateIrradiated {get; set;}
    public string? GeneCode {get; set;}

    /// <summary>
    /// Converts the current object to a participant management EF model object
    /// </summary>
    /// <param name="geneCodeFk">The foreign key of the gene code</param>
    /// <param name="higherRiskReasonFk">The foreign key of the higher risk reason</param>
    /// <returns>A ParticipantManagment object</returns>
    /// <exception cref="ArgumentException"></exception>
    public ParticipantManagement ToParticipantManagement(ParticipantManagement dbParticipant, long? geneCodeFk, long? higherRiskReasonFk)
    {
        dbParticipant.NextTestDueDate = ConvertDateToDateTime(NextTestDueDate);
        dbParticipant.NextTestDueDateCalcMethod = NextTestDueDateCalculationMethod;
        dbParticipant.ParticipantScreeningStatus = ParticipantScreeningStatus;
        dbParticipant.ScreeningCeasedReason = ScreeningCeasedReason;
        dbParticipant.IsHigherRisk = IsHigherRisk ?? dbParticipant.IsHigherRisk;
        dbParticipant.IsHigherRiskActive = IsHigherRiskActive ?? dbParticipant.IsHigherRiskActive;
        dbParticipant.RecordUpdateDateTime = SrcSysProcessedDateTime;
        dbParticipant.HigherRiskNextTestDueDate = ConvertDateToDateTime(HigherRiskNextTestDueDate);
        dbParticipant.HigherRiskReferralReasonId = higherRiskReasonFk ?? dbParticipant.HigherRiskReferralReasonId;
        dbParticipant.DateIrradiated = ConvertDateToDateTime(DateIrradiated);
        dbParticipant.GeneCodeId = geneCodeFk ?? dbParticipant.GeneCodeId;

        return dbParticipant;
    }

    /// <summary>
    /// Converts a nullable DateOnly to a nullable DateTime.
    /// The built in conversion method is only available for the non-nullable DateOnly type.
    /// </summary>
    private static DateTime? ConvertDateToDateTime(DateOnly? date)
    {
        if (date == null) return null;

        DateOnly dateOnly = (DateOnly) date;

        return dateOnly.ToDateTime(TimeOnly.MinValue);
    }
}