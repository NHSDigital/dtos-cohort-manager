namespace NHS.CohortManager.CohortDistributionServices;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.DurableTask.Client;
using Microsoft.DurableTask;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Options;
using Model;
using System.Text.Json;
using Common;
using Activities = DistributeParticipantActivities;
using Model.DTO;

public class DistributeParticipant
{
    private readonly ILogger<DistributeParticipant> _logger;
    private readonly DistributeParticipantConfig _config;
    private readonly IExceptionHandler _exceptionHandler;
    private readonly IHttpClientFunction _httpClientFunction;

    public DistributeParticipant(ILogger<DistributeParticipant> logger, IOptions<DistributeParticipantConfig> config,
                                IExceptionHandler exceptionHandler, IHttpClientFunction httpClientFunction)
    {
        _logger = logger;
        _config = config.Value;
        _exceptionHandler = exceptionHandler;
        _httpClientFunction = httpClientFunction;
    }

    /// <summary>
    /// Service Bus triggered start function
    /// </summary>
    /// <param name="messageBody"></param>
    /// <param name="durableClient"></param>
    /// <param name="functionContext"></param>
    /// <returns></returns>
    [Function("DistributeParticipant")]
    public async Task Run(
   [ServiceBusTrigger(topicName: "%CohortDistributionTopic%", subscriptionName: "%DistributeParticipantSubscription%", Connection = "ServiceBusConnectionString_internal")]
    string messageBody,
    [DurableClient] DurableTaskClient durableClient,
    FunctionContext functionContext)
    {
        try
        {
            var participantRecord = JsonSerializer.Deserialize<BasicParticipantCsvRecord>(messageBody);

            if (string.IsNullOrWhiteSpace(participantRecord.BasicParticipantData.ScreeningId) || string.IsNullOrWhiteSpace(participantRecord.BasicParticipantData.NhsNumber))
            {
                await HandleExceptionAsync(new ArgumentException("One or more of the required parameters is missing"), participantRecord);
                return;
            }

            string instanceId = await durableClient.ScheduleNewOrchestrationInstanceAsync(nameof(DistributeParticipantOrchestrator), participantRecord);

            _logger.LogInformation("Started orchestration with ID = '{instanceId}'.", instanceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start distribute participant");
            await _exceptionHandler.CreateSystemExceptionLog(ex, new Participant(), "Unknown");
        }
    }

    /// <summary>
    /// Orchestrator for the participant distribution process
    /// </summary>
    /// <param name="context">Function context containing a <see cref="BasicParticipantCsvRecord"/></param>
    [Function(nameof(DistributeParticipantOrchestrator))]
    public async Task DistributeParticipantOrchestrator([OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var participantRecord = context.GetInput<BasicParticipantCsvRecord>();
        try
        {
            // Retrieve participant data
            var participantData = await context.CallActivityAsync<CohortDistributionParticipant>(nameof(Activities.RetrieveParticipantData), participantRecord.BasicParticipantData);
            if (participantData is null)
            {
                await HandleExceptionAsync(new KeyNotFoundException("Could not find participant data"), participantRecord);
                return;
            }

            // Check if participant has exceptions
            _logger.LogInformation("Environment variable IgnoreParticipantExceptions is set to {IgnoreParticipantExceptions}", _config.IgnoreParticipantExceptions);
            if (participantData.ExceptionFlag == 1 && !_config.IgnoreParticipantExceptions)
            {
                await HandleExceptionAsync(new ArgumentException("Participant has an unresolved exception, will not add to cohort distribution"), participantRecord);
                return;
            }

            ValidationRecord validationRecord = new() { FileName = participantRecord.FileName, Participant = participantData };

            // Allocate service provider
            validationRecord.ServiceProvider = await context.CallActivityAsync<string>(nameof(Activities.AllocateServiceProvider), participantRecord.Participant);

            // Validation & Transformation
            var transformedParticipant = await context.CallSubOrchestratorAsync<CohortDistributionParticipant?>(nameof(ValidateParticipant.ValidationOrchestrator), validationRecord);
            if (transformedParticipant is null)
            {
                _logger.LogError("Failed to transform participant");
                return;
            }

            _logger.LogInformation("Validation has passed or exceptions are ignored, participant will be added to the database");

            // Add to cohort distribution table
            var participantAdded = await context.CallActivityAsync<bool>(nameof(Activities.AddParticipant), transformedParticipant);
            if (!participantAdded)
            {
                await HandleExceptionAsync(new InvalidOperationException("Failed to add participant to the table"), participantRecord);
                return;
            }

            _logger.LogInformation(
                "Participant has been successfully put on the cohort distribution table. Participant Id: {ParticipantId}, Screening Id: {ScreeningId}, Source: {FileName}",
                participantRecord.Participant.ParticipantId, participantRecord.Participant.ScreeningId, participantRecord.FileName);

            await HandleGpCodeProcessing(context, transformedParticipant);
            // If the participant came from ServiceNow, a request needs to be sent to update the ServiceNow case
            if (participantRecord.Participant.ReferralFlag == "1")
            {
                // In this scenario, the FileName property should be holding the ServiceNow Case Number
                await context.CallActivityAsync(nameof(Activities.SendServiceNowMessage), participantRecord.FileName);
            }
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex, participantRecord);
        }
    }

    /// <summary>
    /// Handles GP code processing for participants with dummy GP codes
    /// Covers both ADD (ServiceNow with ZZZ codes) and AMEND (PDS updates) scenarios
    /// </summary>
    /// <param name="context">Orchestration context</param>
    /// <param name="participant">The participant data</param>
    private async Task HandleGpCodeProcessing(TaskOrchestrationContext context, CohortDistributionParticipant participant)
    {
        bool isAddScenario = participant.ReferralFlag == true;

        if (isAddScenario && !CheckIfHasDummyGpCode(participant)) return;
        if (!isAddScenario && string.IsNullOrEmpty(participant.PrimaryCareProvider)) return;

        var logMessage = isAddScenario
            ? "ADD participant with NHS Number: {NhsNumber} has dummy GP code: {GpCode}, updating Cohort Distribution table"
            : "AMEND participant with NHS Number: {NhsNumber}, overwriting Primary Care Provider with PDS data: {UpdatedGpCode}";

        _logger.LogInformation(logMessage, participant.NhsNumber, participant.PrimaryCareProvider);

        var gpUpdateRequest = new GpCodeUpdateRequestDto
        {
            NhsNumber = participant.NhsNumber,
            ParticipantId = participant.ParticipantId!,
            PrimaryCareProvider = participant.PrimaryCareProvider!,
            IsAmendParticipant = !isAddScenario
        };

        await context.CallActivityAsync(nameof(Activities.UpdateCohortDistributionGpCode), gpUpdateRequest);
    }

    /// <summary>
    /// Checks if the participant has a dummy GP code (starts with ZZZ)
    /// </summary>
    /// <param name="participant">The participant to check</param>
    /// <returns>True if has dummy GP code, false otherwise</returns>
    private static bool CheckIfHasDummyGpCode(CohortDistributionParticipant participant)
    {
        return !string.IsNullOrEmpty(participant.PrimaryCareProvider) &&
               participant.PrimaryCareProvider.StartsWith("ZZZ", StringComparison.OrdinalIgnoreCase);
    }

    private async Task HandleExceptionAsync(Exception ex, BasicParticipantCsvRecord participantRecord)
    {
        _logger.LogError(ex, "Distribute Participant failed");
        await _exceptionHandler.CreateSystemExceptionLog(ex, participantRecord.BasicParticipantData, participantRecord.FileName);
    }
}

