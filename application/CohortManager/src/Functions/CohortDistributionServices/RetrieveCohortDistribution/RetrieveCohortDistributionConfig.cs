namespace NHS.CohortManager.CohortDistributionDataServices;

using System.ComponentModel.DataAnnotations;

public class RetrieveCohortDistributionConfig
{

    [Required]
    public required string ExceptionFunctionURL { get; set; }
    [Required]
    public required string CohortDistributionDataServiceURL { get; set; }
    [Required]
    public required string BsSelectRequestAuditDataService { get; set; }
    public int MaxRowCount { get; set; } = 1_000;

}
