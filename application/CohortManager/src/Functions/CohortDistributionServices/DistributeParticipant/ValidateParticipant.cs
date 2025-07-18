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
    /// Orchestration for the validation and trasnformation process
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
            var previousRecord = await context.CallActivityAsync<CohortDistributionParticipant>(nameof(GetCohortDistributionRecord), validationRecord.Participant.ParticipantId);
            validationRecord.PreviousParticipantRecord = previousRecord;

            // Lookup & Static Validation
            _logger.LogInformation("Validating participant");
            ValidationExceptionLog[] validationResults = await Task.WhenAll(
                context.CallActivityAsync<ValidationExceptionLog>(nameof(StaticValidation), validationRecord),
                context.CallActivityAsync<ValidationExceptionLog>(nameof(LookupValidation), validationRecord)
            );

            var validationResult = new ValidationExceptionLog(validationResults[0], validationResults[1]);

            // Update exception flag and return
            if (validationResult.CreatedException)
            {
                _logger.LogError("Participant {ParticipantId} triggered a validation rule", validationRecord.Participant.ParticipantId);

                await context.CallActivityAsync(nameof(UpdateExceptionFlag), validationRecord.Participant.ParticipantId);

                if (!_config.IgnoreParticipantExceptions)
                {
                    return null;
                }
            }

            // Transformation
            _logger.LogInformation("Transforming participant");
            var transformedParticipant = await context.CallActivityAsync<CohortDistributionParticipant?>(nameof(TransformParticipant), validationRecord);
            transformedParticipant.RecordInsertDateTime = previousRecord.RecordInsertDateTime;

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
    /// Calls static validation
    /// </summary>
    /// <param name="validationRecord"></param>
    /// <returns>A <see cref="ValidationExceptionLog"/> representing if the participant has triggered a rule</returns>
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

    /// <summary>
    /// Calls lookup validation
    /// </summary>
    /// <param name="validationRecord"></param>
    /// <returns>A <see cref="ValidationExceptionLog"/> representing if the participant has triggered a rule</returns>
    [Function(nameof(LookupValidation))]
    public async Task<ValidationExceptionLog> LookupValidation([ActivityTrigger] ValidationRecord validationRecord)
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
        string body = await _httpClient.GetResponseText(response);

        var exceptionLog = JsonSerializer.Deserialize<ValidationExceptionLog>(body);
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
    /// Updates the exception flag in participant management
    /// </summary>
    /// <param name="participantId"></param>
    /// <exception cref="IOException">Thrown if the update fails</exception>
    [Function(nameof(UpdateExceptionFlag))]
    public async Task UpdateExceptionFlag([ActivityTrigger] string participantId)
    {
        var participantManagement = await _participantManagementClient.GetSingle(participantId);
        participantManagement.ExceptionFlag = 1;

        var exceptionFlagUpdated = await _participantManagementClient.Update(participantManagement);
        if (!exceptionFlagUpdated)
        {
            throw new IOException("Failed to update exception flag");
        }
    }
}