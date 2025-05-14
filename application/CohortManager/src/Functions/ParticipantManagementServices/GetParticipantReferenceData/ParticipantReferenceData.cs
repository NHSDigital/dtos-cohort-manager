namespace NHS.CohortManager.ParticipantManagementService;

public class ParticipantReferenceData
{
    public Dictionary<string, string> GeneCodeDescriptions { get; set; }
    public Dictionary<string, string> HigherRiskReferralReasonCodeDescriptions { get; set; }

    public ParticipantReferenceData(Dictionary<string, string> geneCodeDescriptions, Dictionary<string, string> higherRiskReferralReasonCodeDescriptions)
    {
        GeneCodeDescriptions = geneCodeDescriptions;
        HigherRiskReferralReasonCodeDescriptions = higherRiskReferralReasonCodeDescriptions;
    }
}
