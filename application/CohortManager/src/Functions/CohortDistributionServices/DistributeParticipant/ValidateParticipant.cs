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
using Newtonsoft.Json.Linq;
using RulesEngine.Models;
using Azure.Core;

public class ValidateParticipant
{
    private readonly IHttpClientFunction _httpClient;
    private readonly ILogger<ValidateParticipant> _logger;
    private readonly DistributeParticipantConfig _config;
    private readonly IDataServiceClient<ParticipantManagement> _participantManagementClient;
    private readonly IDataServiceClient<CohortDistribution> _cohortDistributionClient;
    private readonly IExceptionHandler _exceptionHandler;

    public ValidateParticipant(IDataServiceClient<CohortDistribution> cohortDistributionClient,
                                       IDataServiceClient<ParticipantManagement> participantManagementClient,
                                       IOptions<DistributeParticipantConfig> config,
                                       IHttpClientFunction httpClientFunction,
                                       ILogger<ValidateParticipant> logger,
                                       IExceptionHandler exceptionHandler)
    {
        _cohortDistributionClient = cohortDistributionClient;
        _participantManagementClient = participantManagementClient;
        _config = config.Value;
        _httpClient = httpClientFunction;
        _logger = logger;
        _exceptionHandler = exceptionHandler;
    }

    /// <summary>
    /// Orchestrator for the validation and trasnformation process
    /// </summary>
    /// <param name="context">Context containing a validation record</param>
    /// <returns>The transformed <see cref="CohortDistributionParticipant"/>, or null if the validation/ transformation failed</returns>
    [Function(nameof(ValidationOrchestrator))]
    public async Task<CohortDistributionParticipant?> ValidationOrchestrator([OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var validationRecord = context.GetInput<ValidationRecord>();

        try
        {
            // Get Previous Record From Cohort Distribution
            _logger.LogInformation("Getting previous record from the Cohort Distribution table");
            var previousRecordTask = context.CallActivityAsync<CohortDistributionParticipant>(nameof(GetCohortDistributionRecord), validationRecord.Participant.ParticipantId);

            // Remove Previous Validation Errors from DB
            var removeValidationRecordTask = context.CallActivityAsync( nameof(RemoveOldValidationExceptions), new OldExceptionRecord()
            {
                NhsNumber = validationRecord.Participant.NhsNumber,
                ScreeningName = validationRecord.Participant.ScreeningName
            });

            CohortDistributionParticipant previousRecord = await previousRecordTask;
            await removeValidationRecordTask;

            validationRecord.PreviousParticipantRecord = previousRecord;
            
            // Lookup & Static Validation
            var lookupTaskOptions = TaskOptions.FromRetryPolicy(new RetryPolicy(
                maxNumberOfAttempts: _config.MaxLookupValidationRetries,
                firstRetryInterval: TimeSpan.FromSeconds(5),
                backoffCoefficient: 2.0));

            _logger.LogInformation("Validating participant");
            var staticTask = context.CallActivityAsync<List<ValidationRuleResult>>(nameof(StaticValidation), validationRecord);
            var lookupTask = context.CallActivityAsync<List<ValidationRuleResult>>(nameof(LookupValidation), validationRecord, lookupTaskOptions);

            await Task.WhenAll(staticTask, lookupTask);

            var validationResult = staticTask.Result.Concat(lookupTask.Result).ToList();

            // Update exception flag and return
            if (validationResult.Any())
            {
                _logger.LogWarning("Participant {ParticipantId} triggered a validation rule", validationRecord.Participant.ParticipantId);

                await context.CallActivityAsync(nameof(HandleValidationExceptions), new ValidationExceptionRecord(validationRecord, validationResult));

                if (!_config.IgnoreParticipantExceptions)
                {
                    return null;
                }
            }

            // Transformation
            _logger.LogInformation("Transforming participant");
            var transformedParticipant = await context.CallActivityAsync<CohortDistributionParticipant?>(nameof(TransformParticipant), validationRecord);

            return transformedParticipant;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate participant {ParticipantId}", validationRecord.Participant.ParticipantId);
            await _exceptionHandler.CreateSystemExceptionLog(ex, new Participant(validationRecord.Participant), validationRecord.FileName);
            return null;
        }
    }

    /// <summary>
    /// Gets the most recent cohort distribution record if there is one
    /// </summary>
    /// <param name="participantId">The participant ID of the participant, from the participant management table</param>
    /// <returns>The most recent <see cref="CohortDistributionParticipant"/> record, or a new empty record</returns>
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

    /// <summary>
    /// Calls the remove old validation exception function
    /// </summary>
    [Function(nameof(RemoveOldValidationExceptions))]
    public async Task RemoveOldValidationExceptions([ActivityTrigger] OldExceptionRecord request)
    {
        string json = JsonSerializer.Serialize(request);
        await _httpClient.SendPost(_config.RemoveOldValidationRecordUrl, json);
    }

    /// <summary>
    /// Calls static validation
    /// </summary>
    /// <param name="validationRecord"></param>
    /// <returns>A <see cref="ValidationExceptionLog"/> representing if the participant has triggered a rule</returns>
    [Function(nameof(StaticValidation))]
    public async Task<List<ValidationRuleResult>?> StaticValidation([ActivityTrigger] ValidationRecord validationRecord)
    {
        var request = new ParticipantCsvRecord
        {
            Participant = new Participant(validationRecord.Participant),
            FileName = validationRecord.FileName
        };

        var json = JsonSerializer.Serialize(request);

        var response = await _httpClient.SendPost(_config.StaticValidationURL, json);
        response.EnsureSuccessStatusCode();

        if (response.StatusCode == HttpStatusCode.NoContent)
        {
            return new List<ValidationRuleResult>();
        }
        string body = await _httpClient.GetResponseText(response);

        var exceptionLog = JsonSerializer.Deserialize<List<ValidationRuleResult>>(body);

        return exceptionLog;
    }

    /// <summary>
    /// Calls lookup validation
    /// </summary>
    /// <param name="validationRecord"></param>
    /// <returns>A <see cref="ValidationExceptionLog"/> representing if the participant has triggered a rule</returns>
    [Function(nameof(LookupValidation))]
    public async Task<List<ValidationRuleResult>> LookupValidation([ActivityTrigger] ValidationRecord validationRecord)
    {
        var request = new LookupValidationRequestBody
        {
            NewParticipant = new Participant(validationRecord.Participant),
            ExistingParticipant = new Participant(validationRecord.PreviousParticipantRecord),
            FileName = validationRecord.FileName
        };

        var json = JsonSerializer.Serialize(request);

        var response = await _httpClient.SendPost(_config.LookupValidationURL, json);
        response.EnsureSuccessStatusCode();
        if (response.StatusCode == HttpStatusCode.NoContent)
        {
            return new List<ValidationRuleResult>();
        }

        string body = await _httpClient.GetResponseText(response);

        var exceptionLog = JsonSerializer.Deserialize<List<ValidationRuleResult>>(body);
        return exceptionLog;
    }

    /// <summary>
    /// Calls the Transform Data Service and returns the transformed participant
    /// </summary>
    /// <returns>
    /// The transformed <see cref="CohortDistributionParticipant"/>, or null if there were exceptions during the transformation process
    /// </returns>
    [Function(nameof(TransformParticipant))]
    public async Task<CohortDistributionParticipant?> TransformParticipant([ActivityTrigger] ValidationRecord validationRecord)
    {
        var transformDataRequestBody = new TransformDataRequestBody()
        {
            Participant = validationRecord.Participant,
            // TODO: is this used?
            ServiceProvider = validationRecord.ServiceProvider,
            ExistingParticipant = validationRecord.PreviousParticipantRecord.ToCohortDistribution()
        };

        var json = JsonSerializer.Serialize(transformDataRequestBody);

        var response = await _httpClient.SendPost(_config.TransformDataServiceURL, json);
        response.EnsureSuccessStatusCode();
        string body = await _httpClient.GetResponseText(response);

        if (string.IsNullOrEmpty(body)) return null;

        return JsonSerializer.Deserialize<CohortDistributionParticipant>(body);
    }

    /// <summary>
    /// Creates a validation excpetion and updates the exception flag
    /// in participant management
    /// </summary>
    /// <exception cref="IOException">Thrown if the update fails</exception>
    [Function(nameof(HandleValidationExceptions))]
    public async Task HandleValidationExceptions([ActivityTrigger] ValidationExceptionRecord validationExceptionRecord)
    {
        // Send exceptions to DB
        ParticipantCsvRecord participantRecord = new()
        {
            Participant = new Participant(validationExceptionRecord.ValidationRecord.Participant),
            FileName = validationExceptionRecord.ValidationRecord.FileName
        };

        var exceptionCreated = await _exceptionHandler.CreateValidationExceptionLog(validationExceptionRecord.ValidationExceptions, participantRecord);

        var participantManagement = await _participantManagementClient.GetSingle(participantRecord.Participant.ParticipantId);
        participantManagement.ExceptionFlag = 1;

        var exceptionFlagUpdated = await _participantManagementClient.Update(participantManagement);
        if (!exceptionFlagUpdated)
        {
            throw new IOException("Failed to update exception flag");
        }
        _logger.LogInformation("Created validation exception and set exception flag to 1 for participant {ParticipantId}", participantRecord.Participant.ParticipantId);
    }
}