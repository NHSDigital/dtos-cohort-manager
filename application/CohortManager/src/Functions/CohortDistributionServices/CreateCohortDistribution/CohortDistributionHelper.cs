namespace NHS.CohortManager.CohortDistributionService;

using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Model;
using Model.Enums;
using Common;
using Microsoft.Extensions.Options;

public class CohortDistributionHelper : ICohortDistributionHelper
{
    private readonly IHttpClientFunction _httpClientFunction;
    private readonly ILogger<CohortDistributionHelper> _logger;
    private readonly CreateCohortDistributionConfig _config;

    public CohortDistributionHelper(IHttpClientFunction httpClientFunction, ILogger<CohortDistributionHelper> logger, IOptions<CreateCohortDistributionConfig> config)
    {
        _httpClientFunction = httpClientFunction;
        _logger = logger;
        _config = config.Value;
    }

    /// <summary>
    /// Calls retrieve participant data which constructs a CohortDistributionParticipant
    /// based on the data from the Participant Management and Demographic tables.
    /// </summary>
    /// <returns>
    /// CohortDistributionParticipant, or null if there were any exceptions during execution.
    /// </returns>
    public async Task<CohortDistributionParticipant?> RetrieveParticipantDataAsync(CreateCohortDistributionRequestBody cohortDistributionRequestBody)
    {
        var retrieveParticipantRequestBody = new RetrieveParticipantRequestBody()
        {
            NhsNumber = cohortDistributionRequestBody.NhsNumber,
            ScreeningService = cohortDistributionRequestBody.ScreeningService
        };

        var requestBody = JsonSerializer.Serialize(retrieveParticipantRequestBody);
        var response = await GetResponseAsync(requestBody, _config.RetrieveParticipantDataURL);

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

        var response = await GetResponseAsync(json, _config.AllocateScreeningProviderURL);

        if (!string.IsNullOrEmpty(response))
        {
            return response;
        }
        return null;

    }

    /// <summary>
    /// Calls the Transform Data Service and returns the transformed participant
    /// </summary>
    /// <returns>
    /// The transformed CohortDistributionParticipant, or null if there were any exceptions during execution.
    /// </returns>
    public async Task<CohortDistributionParticipant?> TransformParticipantAsync(string serviceProvider,
                                                                            CohortDistributionParticipant participantData,
                                                                            CohortDistributionParticipant existingParticipant)
    {
        var transformDataRequestBody = new TransformDataRequestBody()
        {
            Participant = participantData,
            ServiceProvider = serviceProvider,
            ExistingParticipant = existingParticipant.ToCohortDistribution()
        };

        var json = JsonSerializer.Serialize(transformDataRequestBody);

        _logger.LogInformation("Called transform data service");
        var response = await GetResponseAsync(json, _config.TransformDataServiceURL);
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
            FileName = fileName
        };

        var json = JsonSerializer.Serialize(request);

        _logger.LogInformation("Called cohort validation service");
        var response = await GetResponseAsync(json, _config.LookupValidationURL);

        if (!string.IsNullOrEmpty(response))
        {
            return JsonSerializer.Deserialize<ValidationExceptionLog>(response);
        }
        return null;
    }

    private async Task<string> GetResponseAsync(string requestBodyJson, string functionURL)
    {
        var response = await _httpClientFunction.SendPost(functionURL, requestBodyJson);
        if (response == null)
        {
            return "";
        }

        if (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Created)
        {
            var responseText = await _httpClientFunction.GetResponseText(response);
            if (!string.IsNullOrEmpty(responseText))
            {
                return responseText;
            }

        }

        return "";
    }
}
