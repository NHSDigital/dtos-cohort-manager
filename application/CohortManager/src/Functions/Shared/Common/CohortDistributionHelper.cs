namespace Common;

using System.Net;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Model;
using NHS.CohortManager.CohortDistribution;

public class CohortDistributionHelper : ICohortDistributionHelper
{
    private readonly ICallFunction _callFunction;
    private readonly IExceptionHandler _handleException;
    private readonly ILogger<CohortDistributionHelper> _logger;
    public CohortDistributionHelper(ICallFunction callFunction, IExceptionHandler exceptionHandler, ILogger<CohortDistributionHelper> logger)
    {
        _callFunction = callFunction;
        _handleException = exceptionHandler;
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

    public async Task<ValidationExceptionLog> ValidateCohortDistributionRecordAsync(string nhsNumber, string FileName, CohortDistributionParticipant cohortDistributionParticipant)
    {
        var lookupValidationRequestBody = new ValidateCohortDistributionRecordBody()
        {
            NhsNumber = nhsNumber,
            FileName = FileName,
            CohortDistributionParticipant = cohortDistributionParticipant,
        };
        var json = JsonSerializer.Serialize(lookupValidationRequestBody);

        _logger.LogInformation("Called cohort validation service");
        var response = await GetResponseAsync(json, Environment.GetEnvironmentVariable("ValidateCohortDistributionRecordURL"));
        return JsonSerializer.Deserialize<ValidationExceptionLog>(response);
    }

    public async Task<ValidationExceptionLog> ValidateStaticeData(Participant participant, string fileName)
    {

        var json = JsonSerializer.Serialize(new ParticipantCsvRecord()
        {
            Participant = participant,
            FileName = fileName
        });

        if (string.IsNullOrWhiteSpace(participant.ScreeningName))
        {
            var errorDescription = $"A record with Nhs Number: {participant.NhsNumber} has invalid screening name and therefore cannot be processed by the static validation function";
            await _handleException.CreateRecordValidationExceptionLog(participant.NhsNumber!, fileName, errorDescription, "", JsonSerializer.Serialize(participant));

            return new ValidationExceptionLog()
            {
                IsFatal = false,
                CreatedException = true
            };
        }

        var response = await _callFunction.SendPost(Environment.GetEnvironmentVariable("StaticValidationURL")!, json);
        var responseBodyJson = await _callFunction.GetResponseText(response);
        var responseBody = JsonSerializer.Deserialize<ValidationExceptionLog>(responseBodyJson);

        return responseBody!;

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
