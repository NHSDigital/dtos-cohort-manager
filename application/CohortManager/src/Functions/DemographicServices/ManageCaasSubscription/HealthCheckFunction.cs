namespace NHS.CohortManager.DemographicServices;

using System.Threading.Tasks;
using HealthChecks.Extensions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

public class HealthCheckFunction
{
    private readonly HealthCheckService _healthCheckService;

    public HealthCheckFunction(HealthCheckService healthCheckService)
    {
        _healthCheckService = healthCheckService;
    }

    [Function("health")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
    {
        return await HealthCheckServiceExtensions.CreateHealthCheckResponseAsync(req, _healthCheckService);
    }
}

