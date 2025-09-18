namespace NHS.CohortManager.DemographicServices;

using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Collections.Generic;

public static class WireMockAdminHelper
{
    /// <summary>
    /// Seed a default success mapping for Mesh outbox in WireMock so manual tests succeed with a dynamic messageId.
    /// No-op if <paramref name="wireMockAdminUrl"/> is null/empty.
    /// </summary>
    public static async Task SeedMeshSuccessMappingAsync(ILogger logger, string? wireMockAdminUrl)
    {
        if (string.IsNullOrWhiteSpace(wireMockAdminUrl))
        {
            return;
        }

        try
        {
            using var http = new HttpClient();
            var admin = wireMockAdminUrl!.TrimEnd('/');
            var mappingsUrl = $"{admin}/mappings";
            var body = new
            {
                priority = 5,
                request = new { method = "POST", urlPattern = ".*messageexchange/.*/outbox.*" },
                response = new
                {
                    status = 200,
                    jsonBody = new { messageId = "{{randomValue length=24 type='ALPHANUMERIC'}}" },
                    headers = new Dictionary<string, string> { { "Content-Type", "application/json" } },
                    transformers = new[] { "response-template" }
                }
            };
            var json = JsonSerializer.Serialize(body);
            var resp = await http.PostAsync(mappingsUrl, new StringContent(json, Encoding.UTF8, "application/json"));
            if (resp.IsSuccessStatusCode)
            {
                logger.LogInformation("WireMock success mapping seeded at {Url}", mappingsUrl);
            }
            else
            {
                logger.LogWarning("Failed to seed WireMock mapping: {Status} {Text}", (int)resp.StatusCode, await resp.Content.ReadAsStringAsync());
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error seeding WireMock success mapping");
        }
    }
}
