namespace NHS.CohortManager.CohortDistributionService;

using System.ComponentModel.DataAnnotations;

public class TransformDataServiceConfig
{
    [Required]
    public string ExceptionFunctionURL { get; set; }
    [Required]
    public string BsSelectOutCodeUrl { get; set; }
    [Required]
    public string BsSelectGpPracticeUrl { get; set; }
    [Required]
    public string LanguageCodeUrl { get; set; }
    [Required]
    public string ExcludedSMULookupUrl { get; set; }
    [Required]
    public string CurrentPostingUrl { get; set; }
    public int CacheTimeOutHours { get; set; } = 24;
}
