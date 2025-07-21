namespace NHS.Screening.DeleteParticipant;

using System.ComponentModel.DataAnnotations;

public class DeleteParticipantConfig
{
    [Required]
    public string CohortDistributionDataServiceUrl {get; set;}
}
