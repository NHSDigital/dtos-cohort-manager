namespace NHS.Screening.ReceiveCaasFile;

using System.ComponentModel.DataAnnotations;

public class ReceiveCaasFileConfig
{
    [Required]
    public string DemographicDataServiceURL { get; set; }
    [Required]
    public string ScreeningLkpDataServiceURL { get; set; }
    [Required]
    public string DemographicURI { get; set; }
    [Required]
    public int BatchSize { get; set; }
    [Required]
    public string AddQueueName { get; set; }
    [Required]
    public string UpdateQueueName { get; set; }
    [Required]
    public string PMSRemoveParticipant { get; set; }
    public bool AllowDeleteRecords { get; set; }
    [Required]
    public int maxNumberOfChecks { get; set; }
    [Required]
    public string caasfolder_STORAGE { get; set; }
    [Required]
    public string inboundBlobName { get; set; }
    [Required]
    public string ServiceBusConnectionString { get; set; }
    public string GetOrchestrationStatusURL { get; set; }
    public bool UseNewFunctions { get; set; } = false;
    public string ParticipantManagementQueueName {get; set;}
}
