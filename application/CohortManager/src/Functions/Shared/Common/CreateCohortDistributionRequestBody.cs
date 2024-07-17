namespace NHS.CohortManager.CohortDistribution;

public class CreateCohortDistributionRequestBody
{
    public string? NhsNumber { get; set; }
    public string? ScreeningService { get; set; }

    public CreateCohortDistributionRequestBody(string nhsNumber, string screeningService)
    {
        NhsNumber = nhsNumber;
        ScreeningService = screeningService;
    }
}
