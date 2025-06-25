namespace NHS.CohortManager.ServiceNowIntegrationService;

using System.ComponentModel.DataAnnotations;

public class ServiceNowCohortLookupConfig
{
    [Required]
    public required string ServiceNowCasesDataServiceURL { get; set; }

    [Required]
    public required string CohortDistributionDataServiceURL { get; set; }
}
