namespace NHS.CohortManager.CohortDistributionService;

using System.ComponentModel.DataAnnotations;

public class TransformDataServiceConfig
{
    [Required]
    public required string ExceptionFunctionURL { get; set; }
    [Required]
    public required string BsSelectOutCodeUrl { get; set; }
    [Required]
    public required string BsSelectGpPracticeUrl { get; set; }
    [Required]
    public required string LanguageCodeUrl { get; set; }
    [Required]
    public required string ExcludedSMULookupUrl { get; set; }
    [Required]
    public required string CurrentPostingUrl { get; set; }
    public int CacheTimeOutHours { get; set; } = 24;
}
