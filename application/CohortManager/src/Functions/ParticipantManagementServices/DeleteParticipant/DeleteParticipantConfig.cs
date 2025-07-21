namespace NHS.Screening.DeleteParticipant;

using System.ComponentModel.DataAnnotations;

public class DeleteParticipantConfig
{
    [Required]
    public required string CohortDistributionDataServiceUrl {get; set;}
}
