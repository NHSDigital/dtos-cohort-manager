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
using Model.Enums;
using Common;
using Activities = DistributeParticipantActivities;

public class DistributeParticipant
{
    private readonly ILogger<DistributeParticipant> _logger;
    private readonly DistributeParticipantConfig _config;
    private readonly IExceptionHandler _exceptionHandler;

    public DistributeParticipant(ILogger<DistributeParticipant> logger, IOptions<DistributeParticipantConfig> config,
                                IExceptionHandler exceptionHandler)
    {
        _logger = logger;
        _config = config.Value;
        _exceptionHandler = exceptionHandler;
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
   [ServiceBusTrigger("%CohortQueueName%", Connection = "ServiceBusConnectionString")] string messageBody,
   [DurableClient] DurableTaskClient durableClient,
   FunctionContext functionContext)
    {
        _logger.LogInformation($"Received message: {messageBody}");
        var participantRecord = JsonSerializer.Deserialize<BasicParticipantCsvRecord>(messageBody);

        if (string.IsNullOrWhiteSpace(participantRecord.BasicParticipantData.ScreeningId) || string.IsNullOrWhiteSpace(participantRecord.BasicParticipantData.NhsNumber))
        {
            await HandleExceptionAsync(new ArgumentException("One or more of the required parameters is missing"), participantRecord);
            return;
        }

        string instanceId = await durableClient.ScheduleNewOrchestrationInstanceAsync(nameof(DistributeParticipantOrchestrator), participantRecord);

        _logger.LogInformation("Started orchestration with ID = '{instanceId}'.", instanceId);
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

            participantData.RecordType = participantRecord.BasicParticipantData.RecordType;

            // Allocate service provider
            string serviceProvider = await context.CallActivityAsync<string>(nameof(Activities.AllocateServiceProvider), participantRecord.Participant);

            // Check if participant has exceptions
            _logger.LogInformation("Environment variable IgnoreParticipantExceptions is set to {IgnoreParticipantExceptions}", _config.IgnoreParticipantExceptions);
            if (participantData.ExceptionFlag == 1 && !_config.IgnoreParticipantExceptions)
            {
                await HandleExceptionAsync(new ArgumentException("Pariticipant has an unresolved exception, will not add to cohort distribution"), participantRecord);
                return;
            }

            // Validation & Transformation
            ValidationRecord validationRecord = new() {FileName = participantRecord.FileName, Participant = participantData}; 
            var transformedParticipant = await context.CallSubOrchestratorAsync<CohortDistributionParticipant?>(nameof(Validation.ValidationOrchestrator), validationRecord);
            if (transformedParticipant is null)
            {
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
            _logger.LogInformation("Participant has been successfully put on the cohort distribution table");
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex, participantRecord);
        }
    }

    private async Task HandleExceptionAsync(Exception ex, BasicParticipantCsvRecord participantRecord)
    {
        _logger.LogError(ex, "Distribute Participant failed");
        await _exceptionHandler.CreateSystemExceptionLog(ex, participantRecord.BasicParticipantData, participantRecord.FileName);
    }
}