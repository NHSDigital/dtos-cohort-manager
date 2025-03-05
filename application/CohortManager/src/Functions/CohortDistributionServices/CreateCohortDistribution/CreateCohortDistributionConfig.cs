namespace NHS.CohortManager.CohortDistributionService;

using System.ComponentModel.DataAnnotations;

public class CreateCohortDistributionConfig
{
    [Required]
    public string RetrieveParticipantDataURL { get; set; }
    [Required]
    public string AllocateScreeningProviderURL { get; set; }
    [Required]
    public string TransformDataServiceURL { get; set; }
    [Required]
    public string AddCohortDistributionURL { get; set; }
    [Required]
    public string ValidateCohortDistributionRecordURL { get; set; }
    [Required]
    public string DtOsDatabaseConnectionString { get; set; }
    [Required]
    public string ExceptionFunctionURL { get; set; }
    [Required]
    public string CohortQueueName { get; set; }
    [Required]
    public string CohortQueueNamePoison { get; set; }
    public string IgnoreParticipantExceptions { get; set; }
}
