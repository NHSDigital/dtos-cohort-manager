using System.ComponentModel.DataAnnotations;

public class RetrieveCohortDistributionConfig
{

    [Required]
    public string ExceptionFunctionURL { get; set; }
    [Required]
    public string CohortDistributionDataServiceURL { get; set; }
    [Required]
    public string BsSelectRequestAuditDataService { get; set; }

}
