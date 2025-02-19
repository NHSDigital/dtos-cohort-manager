using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Net;
using System.Threading.Tasks;

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
        var healthReport = await _healthCheckService.CheckHealthAsync();

        var response = req.CreateResponse(healthReport.Status == HealthStatus.Healthy ? HttpStatusCode.OK : HttpStatusCode.ServiceUnavailable);
        await response.WriteAsJsonAsync(new
        {
            status = healthReport.Status.ToString(),
            details = healthReport.Entries
        });

        return response;
    }
}
