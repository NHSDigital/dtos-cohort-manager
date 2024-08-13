namespace Common;
using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Model;
using NHS.CohortManager.CohortDistribution;

public class CohortDistributionHelper : ICohortDistributionHelper
{
    private readonly ICallFunction _callFunction;
    private readonly ILogger<CohortDistributionHelper> _logger;
    public CohortDistributionHelper(ICallFunction callFunction, ILogger<CohortDistributionHelper> logger)
    {
        _callFunction = callFunction;
        _logger = logger;
    }

    public async Task<CohortDistributionParticipant> RetrieveParticipantDataAsync(CreateCohortDistributionRequestBody cohortDistributionRequestBody)
    {
        var retrieveParticipantRequestBody = new RetrieveParticipantRequestBody()
        {
            NhsNumber = cohortDistributionRequestBody.NhsNumber,
            ScreeningService = cohortDistributionRequestBody.ScreeningService
        };

        var requestBody = JsonSerializer.Serialize(retrieveParticipantRequestBody);
        var response = await GetResponseAsync(requestBody, Environment.GetEnvironmentVariable("RetrieveParticipantDataURL"));

        if (!string.IsNullOrEmpty(response))
        {
            return JsonSerializer.Deserialize<CohortDistributionParticipant>(response);
        }

        return null;
    }

    public async Task<string?> AllocateServiceProviderAsync(string nhsNumber, string screeningAcronym, string postCode)
    {
        var allocationConfigRequestBody = new AllocationConfigRequestBody
        {
            NhsNumber = nhsNumber,
            Postcode = postCode,
            ScreeningAcronym = screeningAcronym
        };

        var json = JsonSerializer.Serialize(allocationConfigRequestBody);

        var response = await GetResponseAsync(json, Environment.GetEnvironmentVariable("AllocateScreeningProviderURL"));

        if (!string.IsNullOrEmpty(response))
        {
            return response;
        }
        return null;

    }

    public async Task<CohortDistributionParticipant> TransformParticipantAsync(string serviceProvider, CohortDistributionParticipant participantData)
    {
        var transformDataRequestBody = new TransformDataRequestBody()
        {
            Participant = participantData,
            ServiceProvider = serviceProvider
        };

        var json = JsonSerializer.Serialize(transformDataRequestBody);

        _logger.LogInformation("Called transform data service");
        var response = await GetResponseAsync(json, Environment.GetEnvironmentVariable("TransformDataServiceURL"));
        if (!string.IsNullOrEmpty(response))
        {
            return JsonSerializer.Deserialize<CohortDistributionParticipant>(response); ;
        }
        return null;
    }

    private async Task<string> GetResponseAsync(string requestBodyJson, string functionURL)
    {
        var response = await _callFunction.SendPost(functionURL, requestBodyJson);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var responseText = await _callFunction.GetResponseText(response);
            if (!string.IsNullOrEmpty(responseText))
            {
                return responseText;
            }

        }
        return "";
    }
}
