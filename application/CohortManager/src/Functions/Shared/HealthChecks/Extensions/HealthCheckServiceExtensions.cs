namespace HealthChecks.Extensions;

using System.Net;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

public static class HealthCheckServiceExtensions
{
    public static async Task<HttpResponseData> CreateHealthCheckResponseAsync(HttpRequestData req, HealthCheckService healthCheckService)
    {
        var healthReport = await healthCheckService.CheckHealthAsync();

        var response = req.CreateResponse(healthReport.Status == HealthStatus.Healthy ? HttpStatusCode.OK : HttpStatusCode.ServiceUnavailable);
        await response.WriteAsJsonAsync(new
        {
            status = healthReport.Status.ToString(),
            details = healthReport.Entries
        });

        return response;
    }
}
