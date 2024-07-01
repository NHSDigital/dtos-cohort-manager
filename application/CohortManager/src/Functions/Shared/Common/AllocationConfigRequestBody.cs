namespace NHS.CohortManager.CohortDistributionService;
public class AllocationConfigRequestBody
{
    public string? NhsNumber { get; set; }
    public string? Postcode { get; set; }
    public string? ScreeningService { get; set; }
}
