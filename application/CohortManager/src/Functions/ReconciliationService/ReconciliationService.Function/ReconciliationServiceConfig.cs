namespace NHS.CohortManager.ReconciliationService;

using System.ComponentModel.DataAnnotations;

public class ReconciliationServiceConfig
{
    [Required]
    public required string CohortDistributionDataServiceUrl { get; set; }
    [Required]
    public required string ExceptionManagementDataServiceURL { get; set; }
}
