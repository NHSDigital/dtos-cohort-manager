namespace NHS.CohortManager.CohortDistributionService;


public class CohortDistributionService
{

    [Function("CohortDistributionService")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
    {
        // call participant data service

        // call service provider allocation service
            // provide postcode, screening service

        // call service provider transform
            // provide participant, demographic, service provider

        // call add aggregation data service

    }
}
