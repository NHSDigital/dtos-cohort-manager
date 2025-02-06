namespace NHS.CohortManager.CohortDistributionService;

using Microsoft.Azure.Functions.Worker;
using Common;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NHS.CohortManager.CohortDistribution;
using Model;
using Model.Enums;
using Data.Database;
using DataServices.Client;

public class CreateCohortDistribution
{
    private readonly ILogger<CreateCohortDistribution> _logger;
    private readonly ICallFunction _callFunction;
    private readonly ICohortDistributionHelper _CohortDistributionHelper;
    private readonly IExceptionHandler _exceptionHandler;
    private readonly IAzureQueueStorageHelper _azureQueueStorageHelper;
    private readonly IDataServiceClient<ParticipantManagement> _participantManagementClient;

    public CreateCohortDistribution(ILogger<CreateCohortDistribution> logger,
           ICallFunction callFunction,
           ICohortDistributionHelper CohortDistributionHelper,
           IExceptionHandler exceptionHandler,
           IDataServiceClient<ParticipantManagement> participantManagementClient,
           IAzureQueueStorageHelper azureQueueStorageHelper)
    {
        _logger = logger;
        _callFunction = callFunction;
        _CohortDistributionHelper = CohortDistributionHelper;
        _exceptionHandler = exceptionHandler;
        _azureQueueStorageHelper = azureQueueStorageHelper;
        _participantManagementClient = participantManagementClient;
    }

    [Function(nameof(CreateCohortDistribution))]
    public async Task RunAsync([QueueTrigger("%CohortQueueName%", Connection = "AzureWebJobsStorage")] CreateCohortDistributionRequestBody basicParticipantCsvRecord)
    {
        if (string.IsNullOrWhiteSpace(basicParticipantCsvRecord.ScreeningService) || string.IsNullOrWhiteSpace(basicParticipantCsvRecord.NhsNumber))
        {
            await HandleErrorResponseAsync("One or more of the required parameters is missing.", null, basicParticipantCsvRecord.FileName);
            return;
        }

        try
        {
            // Retrieve participant data
            var participantData = await _CohortDistributionHelper.RetrieveParticipantDataAsync(basicParticipantCsvRecord);
            if (participantData == null || string.IsNullOrEmpty(participantData.ScreeningServiceId))
            {
                await HandleErrorResponseAsync("Participant data returned from database is missing required fields", participantData, basicParticipantCsvRecord.FileName);
                return;
            }

            _logger.LogInformation("Participant data Screening Id: {participantData}", participantData.ScreeningServiceId);

            // Allocate service provider
            var serviceProvider = EnumHelper.GetDisplayName(ServiceProvider.BSS);
            if (!string.IsNullOrEmpty(participantData.Postcode))
            {
                serviceProvider = await _CohortDistributionHelper.AllocateServiceProviderAsync(basicParticipantCsvRecord.NhsNumber, participantData.ScreeningAcronym, participantData.Postcode, JsonSerializer.Serialize(participantData));
                if (serviceProvider == null)
                {
                    await HandleErrorResponseAsync("Could not get Postcode in Cohort distribution", participantData, basicParticipantCsvRecord.FileName);
                    return;
                }
            }
            
            // Check if participant has exceptions
            bool ignoreParticipantExceptions = Environment.GetEnvironmentVariable("IgnoreParticipantExceptions") == "true";
            bool participantHasException = participantData.ExceptionFlag == 1;

            if (participantHasException && !ignoreParticipantExceptions) // Will only run if IgnoreParticipantExceptions is false.
            {
                await HandleErrorResponseAsync($"Unable to add to cohort distribution. As participant with ParticipantId: {participantData.ParticipantId}. Has an Exception against it",
                                                participantData, basicParticipantCsvRecord.FileName);
                return;
            }

            // Validation
            participantData.RecordType = basicParticipantCsvRecord.RecordType;
            var validationRecordCreated = await _CohortDistributionHelper.ValidateCohortDistributionRecordAsync(basicParticipantCsvRecord.NhsNumber, basicParticipantCsvRecord.FileName, participantData);

            if (validationRecordCreated && !ignoreParticipantExceptions)
            {
                var errorMessage = $"Validation error: A rule triggered a fatal error, preventing the cohort distribution record with participant Id {participantData.ParticipantId} from being added to the database";
                _logger.LogInformation(errorMessage);
                await _exceptionHandler.CreateRecordValidationExceptionLog(participantData.NhsNumber, basicParticipantCsvRecord.FileName, errorMessage, serviceProvider, JsonSerializer.Serialize(participantData));
                return;
            }
            _logger.LogInformation("Validation has passed or exceptions are ignored, the record with participant id: {ParticipantId} will be added to the database", participantData.ParticipantId);

            // Transformation
            var transformedParticipant = await _CohortDistributionHelper.TransformParticipantAsync(serviceProvider, participantData);
            if (transformedParticipant == null)
            {
                await HandleErrorResponseAsync("The transformed participant returned null from the transform participant function", transformedParticipant, basicParticipantCsvRecord.FileName);
                return;
            }

            // Add to cohort distribution table
            var cohortAddResponse = await AddCohortDistribution(transformedParticipant);
            if (cohortAddResponse.StatusCode != HttpStatusCode.OK)
            {
                await HandleErrorResponseAsync("Failed to add the participant to the Cohort Distribution table", transformedParticipant, basicParticipantCsvRecord.FileName);
                return;
            }
            _logger.LogInformation("Participant has been successfully put on the cohort distribution table");
            return;
        }
        catch (Exception ex)
        {
            var errorMessage = $"Failed during TransformParticipant or AddCohortDistribution Function.\nMessage: {ex.Message}\nStack Trace: {ex.StackTrace}";
            _logger.LogError(ex, errorMessage);
            await _exceptionHandler.CreateSystemExceptionLogFromNhsNumber(ex, basicParticipantCsvRecord.NhsNumber, basicParticipantCsvRecord.FileName, "", JsonSerializer.Serialize(basicParticipantCsvRecord.ErrorRecord) ?? "N/A");
            throw;
        }
    }

    private async Task HandleErrorResponseAsync(string errorMessage, CohortDistributionParticipant cohortDistributionParticipant, string fileName)
    {
        _logger.LogError(errorMessage);
        var participant = new Participant();
        if (cohortDistributionParticipant != null)
        {
            participant = new Participant(cohortDistributionParticipant);
        }

        await _exceptionHandler.CreateSystemExceptionLog(new Exception(errorMessage), participant, fileName);
        await _azureQueueStorageHelper.AddItemToQueueAsync<CohortDistributionParticipant>(cohortDistributionParticipant, Environment.GetEnvironmentVariable("CohortQueueNamePoison"));
    }

    private async Task<HttpWebResponse> AddCohortDistribution(CohortDistributionParticipant transformedParticipant)
    {
        transformedParticipant.Extracted = DatabaseHelper.ConvertBoolStringToBoolByType("IsExtractedToBSSelect", DataTypes.Integer).ToString();
        var json = JsonSerializer.Serialize(transformedParticipant);
        var response = await _callFunction.SendPost(Environment.GetEnvironmentVariable("AddCohortDistributionURL"), json);

        _logger.LogInformation("Called {AddCohortDistribution} function", nameof(AddCohortDistribution));
        return response;
    }
}
