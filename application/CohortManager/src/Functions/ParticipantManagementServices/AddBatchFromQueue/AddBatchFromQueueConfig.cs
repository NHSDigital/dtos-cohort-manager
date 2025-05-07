namespace AddBatchFromQueue;

using System.ComponentModel.DataAnnotations;

public class AddBatchFromQueueConfig
{
    [Required]
    public string DtOsDatabaseConnectionString { get; set; }

    [Required]
    public string LookupValidationURL { get; set; }

    [Required]
    public string ExceptionFunctionURL { get; set; }

    [Required]
    public string ParticipantManagementUrl { get; set; }

    [Required]
    public string QueueName { get; set; }

    [Required]
    public string QueueConnectionString { get; set; }

    [Required]
    public string AddQueueName { get; set; }

    [Required]
    public string DemographicURIGet { get; set; }
    [Required]
    public string DSaddParticipant { get; set; }
    [Required]
    public string DSmarkParticipantAsEligible { get; set; }
    [Required]
    public string StaticValidationURL { get; set; }
}
