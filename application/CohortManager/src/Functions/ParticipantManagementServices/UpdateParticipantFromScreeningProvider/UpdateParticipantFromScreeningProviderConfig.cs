namespace NHS.Screening.UpdateParticipantFromScreeningProvider;

using System.ComponentModel.DataAnnotations;

public class UpdateParticipantFromScreeningProviderConfig
{
    [Required]
    public string ParticipantManagementUrl {get; set;}
    [Required]
    public string GeneCodeLkpUrl {get; set;}
    [Required]
    public string HigherRiskReferralReasonLkpUrl {get; set;}
}
