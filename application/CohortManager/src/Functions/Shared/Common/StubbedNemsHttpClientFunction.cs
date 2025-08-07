namespace Common;

using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Common;
using Microsoft.Extensions.Logging;

public class StubbedNemsHttpClientFunction : INemsHttpClientFunction
{
    public string GenerateJwtToken(string asid, string audience, string scope)
    {
        var header = new
        {
            alg = "none",
            typ = "JWT"
        };

        var payload = new
        {
            iss = "https://nems.nhs.uk",
            sub = $"https://fhir.nhs.uk/Id/accredited-system|{asid}",
            aud = audience,
            exp = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds(),
            iat = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            reason_for_request = "directcare",
            scope = scope,
            requesting_system = $"https://fhir.nhs.uk/Id/accredited-system|{asid}"
        };

        var headerJson = JsonSerializer.Serialize(header);
        var payloadJson = JsonSerializer.Serialize(payload);

        var headerEncoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(headerJson))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var payloadEncoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(payloadJson))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');

        // Unsigned JWT (signature is empty)
        return $"{headerEncoded}.{payloadEncoded}.";
    }

    public async Task<HttpResponseMessage> SendSubscriptionDelete(NemsSubscriptionRequest request, int timeoutSeconds = 300)
    {
        await Task.CompletedTask;
        return HttpStubUtilities.CreateFakeHttpResponse(request.Url, "");
    }

    public async Task<HttpResponseMessage> SendSubscriptionPost(NemsSubscriptionPostRequest request, int timeoutSeconds = 300)
    {
        await Task.CompletedTask;
        string subId = $"https://clinicals.spineservices.nhs.uk/STU3/Subscription/{Guid.NewGuid():N}";
        return HttpStubUtilities.CreateFakeHttpResponse(request.Url, "", System.Net.HttpStatusCode.OK, new Uri(subId));

    }
}
