namespace NHS.CohortManager.CohortDistributionServices;

using System.Text.Json;
using Microsoft.Extensions.Logging;
using Common;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Model;
using System.Net;
using Model.Enums;
using DataServices.Client;
using Microsoft.Extensions.Options;

public class Validation
{
    private readonly IHttpClientFunction _httpClient;
    private readonly ILogger<Validation> _logger;
    private readonly DistributeParticipantConfig _config;
    private readonly IDataServiceClient<ParticipantManagement> _participantManagementClient;
    private readonly IDataServiceClient<CohortDistribution> _cohortDistributionClient;
    private readonly IExceptionHandler _exceptionHandler;

    public Validation(IDataServiceClient<CohortDistribution> cohortDistributionClient,
                                       IDataServiceClient<ParticipantManagement> participantManagementClient,
                                       IOptions<DistributeParticipantConfig> config,
                                       IHttpClientFunction httpClientFunction,
                                       ILogger<Validation> logger)
    {
        _cohortDistributionClient = cohortDistributionClient;
        _participantManagementClient = participantManagementClient;
        _config = config.Value;
        _httpClient = httpClientFunction;
        _logger = logger;
    }

    // TODO: retries
    [Function(nameof(ValidationOrchestrator))]
    public async Task<CohortDistributionParticipant?> ValidationOrchestrator([OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var participantRecord = context.GetInput<ValidationRecord>();
        string fileName = context.GetInput<string>();
        try
        {
            // Get Previous Record From Cohort Distribution
            _logger.LogInformation("Getting previous record from the Cohort Distribution table");
            var cohortDistRecord = context.CallActivityAsync<CohortDistributionParticipant>(nameof(GetCohortDistributionRecord), participantRecord.Participant.ParticipantId);

            // Lookup & Static Validation
            _logger.LogInformation("Validating participant");
            ValidationExceptionLog[] validationResults = await Task.WhenAll(
                context.CallActivityAsync<ValidationExceptionLog>(nameof(StaticValidation), participantRecord),
                context.CallActivityAsync<ValidationExceptionLog>(nameof(LookupValidation), participantRecord)
            );

            var validationResult = new ValidationExceptionLog(validationResults[0], validationResults[1]);

            if (validationResult.CreatedException)
            {
                _logger.LogError("Participant {ParticipantId} triggered a validation rule", participantRecord.Participant.ParticipantId);

                bool updated = await context.CallActivityAsync<bool>(nameof(UpdateExceptionFlag), participantRecord.Participant.ParticipantId);

                if (!_config.IgnoreParticipantExceptions)
                {
                    return null;
                }
            }

            // Transformation
            _logger.LogInformation("Transforming participant");
            var transformedParticipant = await context.CallActivityAsync<CohortDistributionParticipant?>(nameof(TransformParticipant), participantRecord);

            return transformedParticipant;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate participant {ParticipantId}", participantRecord.Participant.ParticipantId);
            await _exceptionHandler.CreateSystemExceptionLog(ex, new Participant(participantRecord.Participant), participantRecord.FileName);
            return null;
        }
    }

    [Function(nameof(GetCohortDistributionRecord))]
    public async Task<CohortDistributionParticipant> GetCohortDistributionRecord([ActivityTrigger] string participantId)
    {
        long longParticipantId = long.Parse(participantId);

        var cohortDistRecords = await _cohortDistributionClient.GetByFilter(x => x.ParticipantId == longParticipantId);
        var latestParticipant = cohortDistRecords.OrderByDescending(x => x.CohortDistributionId).FirstOrDefault();

        if (latestParticipant != null)
        {
            return new CohortDistributionParticipant(latestParticipant);
        }
        else
        {
            var participantToReturn = new CohortDistributionParticipant();
            participantToReturn.NhsNumber = "0";

            return participantToReturn;
        }
    }

    [Function(nameof(StaticValidation))]
    public async Task<ValidationExceptionLog> StaticValidation([ActivityTrigger] ValidationRecord validationRecord)
    {
        var request = new ParticipantCsvRecord
        {
            Participant = new Participant(validationRecord.Participant),
            FileName = validationRecord.FileName
        };

        var json = JsonSerializer.Serialize(request);

        var response = await _httpClient.SendPost(_config.StaticValidationURL, json);
        response.EnsureSuccessStatusCode();
        string body = await _httpClient.GetResponseText(response);

        var exceptionLog = JsonSerializer.Deserialize<ValidationExceptionLog>(body);

        return exceptionLog;
    }

    [Function(nameof(LookupValidation))]
    public async Task<ValidationExceptionLog> LookupValidation([ActivityTrigger] string fileName,
                                                    CohortDistributionParticipant requestParticipant,
                                                    CohortDistributionParticipant existingParticipant)
    {
        var lookupRequest = new LookupValidationRequestBody
        {
            NewParticipant = new Participant(requestParticipant),
            ExistingParticipant = new Participant(existingParticipant),
            FileName = fileName,
            RulesType = RulesType.ParticipantManagement
        };

        var cohortRequest = lookupRequest;
        lookupRequest.RulesType = RulesType.CohortDistribution;

        ValidationExceptionLog[] validationResults = await Task.WhenAll(
            CallLookupValidation(lookupRequest),
            CallLookupValidation(cohortRequest)
        );

        return new ValidationExceptionLog(validationResults[0], validationResults[1]);
    }

    /// <summary>
    /// Calls the Transform Data Service and returns the transformed participant
    /// </summary>
    /// <returns>
    /// The transformed CohortDistributionParticipant, or null if there were any exceptions during execution.
    /// </returns>
    [Function(nameof(TransformParticipant))]
    public async Task<CohortDistributionParticipant?> TransformParticipant([ActivityTrigger] string serviceProvider,
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

        var response = await _httpClient.SendPost(_config.TransformDataServiceURL, json);
        response.EnsureSuccessStatusCode();
        string body = await _httpClient.GetResponseText(response);
        if (string.IsNullOrEmpty(body)) return null;

        return JsonSerializer.Deserialize<CohortDistributionParticipant>(body);
    }

    [Function(nameof(UpdateExceptionFlag))]
    public async Task<bool> UpdateExceptionFlag([ActivityTrigger] string participantId)
    {
        var participantManagement = await _participantManagementClient.GetSingle(participantId);
        participantManagement.ExceptionFlag = 1;

        var exceptionFlagUpdated = await _participantManagementClient.Update(participantManagement);
        if (!exceptionFlagUpdated)
        {
            throw new IOException("Failed to update exception flag");
        }

        return true;
    }

    /// <summary>
    /// Calls the lookup validation function
    /// </summary>
    /// <param name="request"></param>
    /// <remarks>Temporary, both calls to lookup validation will be merged</remarks>
    private async Task<ValidationExceptionLog> CallLookupValidation(LookupValidationRequestBody request)
    {
        var json = JsonSerializer.Serialize(request);

        var response = await _httpClient.SendPost(_config.LookupValidationURL, json);
        response.EnsureSuccessStatusCode();
        string body = await _httpClient.GetResponseText(response);

        var exceptionLog = JsonSerializer.Deserialize<ValidationExceptionLog>(body);
        return exceptionLog;
    }
}