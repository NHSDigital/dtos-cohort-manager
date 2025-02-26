namespace NHS.Screening.RetrieveMeshFile;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Net;
using System.Threading.Tasks;
using HealthChecks.Extensions;

public class HealthCheckFunction
{
    private readonly HealthCheckService _healthCheckService;

    public HealthCheckFunction(HealthCheckService healthCheckService)
    {
        _healthCheckService = healthCheckService;
    }

    [Function("health")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req)
    {
        return await HealthCheckServiceExtensions.CreateHealthCheckResponseAsync(req, _healthCheckService);
    }
}
