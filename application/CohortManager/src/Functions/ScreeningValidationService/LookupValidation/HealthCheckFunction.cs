namespace NHS.CohortManager.ScreeningValidationService;

using System.Net;
using System.Threading.Tasks;
using Common.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

public class HealthCheckFunction
{
    private readonly HealthCheckService _healthCheckService;
    private readonly IReadRules _readRules;

    public HealthCheckFunction(HealthCheckService healthCheckService, IReadRules readRules)
    {
        _healthCheckService = healthCheckService;
        _readRules = readRules;
    }

    [Function("health")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
    {
        var healthReport = await _healthCheckService.CheckHealthAsync();
        var json = await _readRules.GetRulesFromDirectory("Breast_Screening_lookupRules.json");
        var response = req.CreateResponse(healthReport.Status == HealthStatus.Healthy ? HttpStatusCode.OK : HttpStatusCode.ServiceUnavailable);
        // Validate configuration
        if (string.IsNullOrEmpty(json))
        {
            await response.WriteAsJsonAsync(new
            {
                name = "HealthCheck for LookupValidation",
                status = HealthStatus.Unhealthy.ToString(),
                details = "The service is down. Lookup Validation file is missing."
            });
        }
        else
        {
            await response.WriteAsJsonAsync(new
            {
                status = healthReport.Status.ToString(),
                details = healthReport.Entries
            });
        }
        return response;
    }
}