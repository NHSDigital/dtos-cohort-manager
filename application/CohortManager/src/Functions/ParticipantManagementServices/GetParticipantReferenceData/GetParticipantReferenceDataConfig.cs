namespace NHS.Screening.GetParticipantReferenceData;

using System.ComponentModel.DataAnnotations;

public class GetParticipantReferenceDataConfig
{
    [Required]
    public string HigherRiskReferralReasonLkpDataServiceUrl {get; set;}
    [Required]
    public string GeneCodeLkpDataServiceUrl {get; set;}
}
