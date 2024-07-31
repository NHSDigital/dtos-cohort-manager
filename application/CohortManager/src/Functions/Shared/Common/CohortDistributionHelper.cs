using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Model;
using NHS.CohortManager.CohortDistribution;

namespace Common;

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
        _logger.LogInformation("Called retrieve participant data service");

        if (string.IsNullOrEmpty(response))
        {
            throw new Exception("there has been a problem getting participant Data");
        }
        _logger.LogInformation("");
        return JsonSerializer.Deserialize<CohortDistributionParticipant>(response);
    }

    public async Task<string> AllocateServiceProviderAsync(CreateCohortDistributionRequestBody requestBody, CohortDistributionParticipant participantData)
    {
        var allocationConfigRequestBody = new AllocationConfigRequestBody
        {
            NhsNumber = requestBody.NhsNumber,
            Postcode = participantData.Postcode,
            ScreeningService = requestBody.ScreeningService
        };

        var json = JsonSerializer.Serialize(allocationConfigRequestBody);


        var response = await GetResponseAsync(json, Environment.GetEnvironmentVariable("AllocateScreeningProviderURL"));
        _logger.LogInformation("Called allocate screening provider service");
        if (!string.IsNullOrEmpty(response))
        {
            return response;
        }
        _logger.LogError("there has been a problem calling the service provider");
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
        _logger.LogInformation("Called allocate screening provider service");
        if (!string.IsNullOrEmpty(response))
        {
            return JsonSerializer.Deserialize<CohortDistributionParticipant>(response);
        }

        _logger.LogError("there has been a problem calling the transform participant");
        return null;
    }

    private async Task<string> GetResponseAsync(string requestBody, string functionURL)
    {
        var json = JsonSerializer.Serialize(requestBody);
        var response = await _callFunction.SendPost(functionURL, json);

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