namespace NHS.Screening.ProcessNemsUpdate;

using System.ComponentModel.DataAnnotations;

public class ProcessNemsUpdateConfig
{
    [Required]
    public required string RetrievePdsDemographicURL { get; set; }
    [Required]
    public required string NemsMessages { get; set; }
    [Required]
    public required string UpdateQueueName { get; set; }
}
