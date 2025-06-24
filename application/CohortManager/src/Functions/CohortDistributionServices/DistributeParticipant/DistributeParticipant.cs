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

    public DistributeParticipant(ILogger<DistributeParticipant> logger, IOptions<DistributeParticipantConfig> config)
    {
        _logger = logger;
        _config = config.Value;
    }

    // TODO: staic validation
    [Function(nameof(MyOrchestration))]
    public async Task MyOrchestration([OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var participantRecord = context.GetInput<BasicParticipantCsvRecord>();
        var outputs = new List<string>();

        _logger.LogInformation("Orchestration started with input: {input}", input);

        if (string.IsNullOrWhiteSpace(participantRecord.ScreeningService) || string.IsNullOrWhiteSpace(participantRecord.NhsNumber))
        {
            await HandleExceptionAsync("One or more of the required parameters is missing.", null, participantRecord.FileName);
            return;
        }

        try
        {
            // Retrieve participant data
            CohortDistributionParticipant participantData = await context.CallActivityAsync(nameof(Activities.RetrieveParticipantData), participantRecord);
            if (participantData == null || string.IsNullOrEmpty(participantData.ScreeningServiceId))
            {
                await HandleExceptionAsync("Participant data returned from database is missing required fields", participantData, participantRecord.FileName);
                return;
            }

            CohortDistributionParticipant previousCohortDistributionRecord = await context.CallActivityAsync(nameof(Activities.GetCohortDistributionRecord), participantData.ParticipantId);

            // Allocate service provider
            var serviceProvider = EnumHelper.GetDisplayName(ServiceProvider.BSS);
            serviceProvider = await context.CallActivityAsync(nameof(Activities.RetrieveParticipantData), participantRecord)
            if (serviceProvider == null)
            {
                await HandleExceptionAsync("Could not allocate participant to service provider from postcode", participantData, participantRecord.FileName);
                return;
            }

            // Check if participant has exceptions
            var ignoreParticipantExceptions = _config.IgnoreParticipantExceptions;
            _logger.LogInformation("Environment variable IgnoreParticipantExceptions is set to {IgnoreParticipantExceptions}", ignoreParticipantExceptions);

            var participantHasException = participantData.ExceptionFlag == 1;
            if (participantHasException && !ignoreParticipantExceptions) // Will only run if IgnoreParticipantExceptions is false.
            {
                await HandleExceptionAsync($"Unable to add to cohort distribution. As participant with ParticipantId: {participantData.ParticipantId}. Has an Exception against it", participantData, participantRecord.FileName!);
                return;
            }

            // Validation
            participantData.RecordType = participantRecord.RecordType;
            var validationResponse = 
            var validationResponse = await _CohortDistributionHelper.ValidateCohortDistributionRecordAsync(participantRecord.FileName!, participantData, previousCohortDistributionRecord);

            // Update participant exception flag
            if (validationResponse.CreatedException)
            {
                var errorMessage = $"Participant {participantData.ParticipantId} triggered a validation rule, so will not be added to cohort distribution";
                await HandleExceptionAsync(errorMessage, participantData, participantRecord.FileName!);

                var participantManagement = await _participantManagementClient.GetSingle(participantData.ParticipantId);
                participantManagement.ExceptionFlag = 1;

                var exceptionFlagUpdated = await _participantManagementClient.Update(participantManagement);
                if (!exceptionFlagUpdated)
                {
                    throw new IOException("Failed to update exception flag");
                }

                if (!ignoreParticipantExceptions)
                {
                    return;
                }
            }
            _logger.LogInformation("Validation has passed or exceptions are ignored, the record with participant id: {ParticipantId} will be added to the database", participantData.ParticipantId);

            // Transformation
            var transformedParticipant = await _CohortDistributionHelper.TransformParticipantAsync(serviceProvider, participantData, previousCohortDistributionRecord);
            if (transformedParticipant == null)
            {
                return;
            }

            // Add to cohort distribution table
            var participantAdded = await AddCohortDistribution(transformedParticipant);
            if (!participantAdded)
            {
                await HandleExceptionAsync("Failed to add the participant to the Cohort Distribution table", transformedParticipant, participantRecord.FileName);
                return;
            }
            _logger.LogInformation("Participant has been successfully put on the cohort distribution table");
        }
        catch (Exception ex)
        {
            var errorMessage = $"Create Cohort Distribution failed .\nMessage: {ex.Message}\nStack Trace: {ex.StackTrace}";
            await HandleExceptionAsync(errorMessage,
                                    new CohortDistributionParticipant { NhsNumber = participantRecord.NhsNumber },
                                    participantRecord.FileName!);
            throw;
        }
    }

    [Function("DistributeParticipant")]
    public async Task Run(
       [ServiceBusTrigger("%CohortQueueName%", Connection = "ServiceBusConnectionString")] string messageBody,
       [DurableClient] DurableTaskClient durableClient,
       FunctionContext functionContext)
    {
        _logger.LogInformation($"Received message: {messageBody}");
        var participantRecord = JsonSerializer.Deserialize<BasicParticipantCsvRecord>(messageBody);

        // Start a new orchestration instance and pass the message body as input.
        string instanceId = await durableClient.ScheduleNewOrchestrationInstanceAsync(nameof(MyOrchestration), participantRecord);

        _logger.LogInformation("Started orchestration with ID = '{instanceId}'.", instanceId);
    }

    private async Task<bool> AddCohortDistribution(CohortDistributionParticipant transformedParticipant)
    {
        transformedParticipant.Extracted = DatabaseHelper.ConvertBoolStringToBoolByType("IsExtractedToBSSelect", DataTypes.Integer).ToString();
        var cohortDistributionParticipantToAdd = transformedParticipant.ToCohortDistribution();
        var isAdded = await _cohortDistributionClient.Add(cohortDistributionParticipantToAdd);

        _logger.LogInformation("sent participant to cohort distribution data service");
        return isAdded;
    }

    private async Task HandleExceptionAsync(string errorMessage, CohortDistributionParticipant cohortDistributionParticipant, string fileName)
    {
        _logger.LogError(errorMessage);
        var participant = new Participant();
        if (cohortDistributionParticipant != null)
        {
            participant = new Participant(cohortDistributionParticipant);
        }

        await _exceptionHandler.CreateSystemExceptionLog(new Exception(errorMessage), participant, fileName);
        await _azureQueueStorageHelper.AddAsync<CohortDistributionParticipant>(cohortDistributionParticipant, _config.CohortQueueNamePoison);
    }
}