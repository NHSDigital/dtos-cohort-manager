namespace NHS.CohortManager.CohortDistributionDataServices;

using System.ComponentModel.DataAnnotations;

public class RetrieveCohortRequestAuditConfig
{

    [Required]
    public string ExceptionFunctionURL { get; set; }
    [Required]
    public string CohortDistributionDataServiceURL { get; set; }
    [Required]
    public string BsSelectRequestAuditDataService { get; set; }

}
