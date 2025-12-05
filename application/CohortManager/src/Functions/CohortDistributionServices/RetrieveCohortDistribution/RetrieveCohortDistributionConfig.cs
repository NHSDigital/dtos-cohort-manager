namespace NHS.CohortManager.CohortDistributionDataServices;

using System.ComponentModel.DataAnnotations;

public class RetrieveCohortDistributionConfig
{

    [Required]
    public string ExceptionFunctionURL { get; set; }
    [Required]
    public string CohortDistributionDataServiceURL { get; set; }
    [Required]
    public string BsSelectRequestAuditDataService { get; set; }
    public int MaxRowCount { get; set; } = 1_000;
    public bool RetrieveSupersededRecordsLast { get; set; } = false;

}
