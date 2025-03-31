namespace NHS.CohortManager.CohortDistribution;

using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Model;
using Model.Enums;
using Common;

public class CohortDistributionHelper : ICohortDistributionHelper
{
    private readonly ICallFunction _callFunction;
    private readonly ILogger<CohortDistributionHelper> _logger;
    private readonly CreateCohortDistributionConfig _config;

    public CohortDistributionHelper(ICallFunction callFunction, ILogger<CohortDistributionHelper> logger)
    {
        _callFunction = callFunction;
        _logger = logger;
    }

    public async Task<CohortDistributionParticipant?> RetrieveParticipantDataAsync(CreateCohortDistributionRequestBody cohortDistributionRequestBody)
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

    public async Task<string?> AllocateServiceProviderAsync(string nhsNumber, string screeningAcronym, string postCode, string errorRecord)
    {
        var allocationConfigRequestBody = new AllocationConfigRequestBody
        {
            NhsNumber = nhsNumber,
            Postcode = postCode,
            ScreeningAcronym = screeningAcronym,
            ErrorRecord = errorRecord
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
            ServiceProvider = serviceProvider,
        };

        var json = JsonSerializer.Serialize(transformDataRequestBody);

        _logger.LogInformation("Called transform data service");
        var response = await GetResponseAsync(json, Environment.GetEnvironmentVariable("TransformDataServiceURL"));
        if (!string.IsNullOrEmpty(response))
        {
            return JsonSerializer.Deserialize<CohortDistributionParticipant>(response);
        }
        return null;
    }

    public async Task<ValidationExceptionLog> ValidateCohortDistributionRecordAsync(string fileName,
                                                                                    CohortDistributionParticipant requestParticipant,
                                                                                    CohortDistributionParticipant existingParticipant)
    {
        var request = new LookupValidationRequestBody
        {
            NewParticipant = new Participant(requestParticipant),
            ExistingParticipant = new Participant(existingParticipant),
            FileName = fileName,
            RulesType = RulesType.CohortDistribution
        };

        var json = JsonSerializer.Serialize(request);

        _logger.LogInformation("Called cohort validation service");
        var response = await GetResponseAsync(json, _config.);
        return JsonSerializer.Deserialize<ValidationExceptionLog>(response);
    }

    private async Task<string> GetResponseAsync(string requestBodyJson, string functionURL)
    {
        var response = await _callFunction.SendPost(functionURL, requestBodyJson);
        if (response == null) 
        {
            return "";
        }

        if (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Created)
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
