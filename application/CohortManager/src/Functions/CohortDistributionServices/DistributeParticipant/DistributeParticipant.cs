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

    // TODO: staic validation
    [Function(nameof(DistributeParticipantOrchestrator))]
    public async Task DistributeParticipantOrchestrator([OrchestrationTrigger] TaskOrchestrationContext context)
    {
        _logger.LogInformation("Orchestration started");
        var participantRecord = context.GetInput<BasicParticipantCsvRecord>();
        try
        {
            // Retrieve participant data
            var participantData = await context.CallActivityAsync<CohortDistributionParticipant>(nameof(Activities.RetrieveParticipantData), participantRecord.BasicParticipantData);
            participantData.RecordType = participantRecord.BasicParticipantData.RecordType;

            // Allocate service provider
            string serviceProvider = await context.CallActivityAsync<string>(nameof(Activities.AllocateServiceProvider), participantRecord);

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