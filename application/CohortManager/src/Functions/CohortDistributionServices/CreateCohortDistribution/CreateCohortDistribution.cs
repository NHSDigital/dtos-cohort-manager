namespace NHS.CohortManager.CohortDistributionService;

using Microsoft.Azure.Functions.Worker;
using Common;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Model;
using Model.Enums;
using Data.Database;
using DataServices.Client;
using Microsoft.Extensions.Options;

public class CreateCohortDistribution
{
    private readonly ILogger<CreateCohortDistribution> _logger;
    private readonly IHttpClientFunction _httpClientFunction;
    private readonly ICohortDistributionHelper _CohortDistributionHelper;
    private readonly IExceptionHandler _exceptionHandler;
    private readonly IAzureQueueStorageHelper _azureQueueStorageHelper;
    private readonly IDataServiceClient<ParticipantManagement> _participantManagementClient;
    private readonly CreateCohortDistributionConfig _config;
    private readonly IDataServiceClient<CohortDistribution> _cohortDistributionClient;

    public CreateCohortDistribution(ILogger<CreateCohortDistribution> logger,
                                    IHttpClientFunction httpClientFunction,
                                    ICohortDistributionHelper CohortDistributionHelper,
                                    IExceptionHandler exceptionHandler,
                                    IAzureQueueStorageHelper azureQueueStorageHelper,
                                    IDataServiceClient<ParticipantManagement> participantManagementClient,
                                    IDataServiceClient<CohortDistribution> cohortDistributionClient,
                                    IOptions<CreateCohortDistributionConfig> createCohortDistributionConfig)
    {
        _logger = logger;
        _httpClientFunction = httpClientFunction;
        _CohortDistributionHelper = CohortDistributionHelper;
        _exceptionHandler = exceptionHandler;
        _azureQueueStorageHelper = azureQueueStorageHelper;
        _participantManagementClient = participantManagementClient;
        _cohortDistributionClient = cohortDistributionClient;
        _config = createCohortDistributionConfig.Value;
    }

    [Function(nameof(CreateCohortDistribution))]
    public async Task RunAsync([QueueTrigger("%CohortQueueName%", Connection = "AzureWebJobsStorage")] CreateCohortDistributionRequestBody basicParticipantCsvRecord)
    {
        if (string.IsNullOrWhiteSpace(basicParticipantCsvRecord.ScreeningService) || string.IsNullOrWhiteSpace(basicParticipantCsvRecord.NhsNumber))
        {
            await HandleExceptionAsync("One or more of the required parameters is missing.", null, basicParticipantCsvRecord.FileName);
            return;
        }

        try
        {
            // Retrieve participant data
            var participantData = await _CohortDistributionHelper.RetrieveParticipantDataAsync(basicParticipantCsvRecord);
            if (participantData == null || string.IsNullOrEmpty(participantData.ScreeningServiceId))
            {
                await HandleExceptionAsync("Participant data returned from database is missing required fields", participantData, basicParticipantCsvRecord.FileName);
                return;
            }

            var previousCohortDistributionRecord = await GetLatestCohortDistributionRecordAsync(participantData.ParticipantId);

            // Allocate service provider
            var serviceProvider = EnumHelper.GetDisplayName(ServiceProvider.BSS);
            if (!string.IsNullOrEmpty(participantData.Postcode))
            {
                serviceProvider = await _CohortDistributionHelper.AllocateServiceProviderAsync(basicParticipantCsvRecord.NhsNumber,
                                                                                            participantData.ScreeningAcronym,
                                                                                            participantData.Postcode,
                                                                                            JsonSerializer.Serialize(participantData));
                if (serviceProvider == null)
                {
                    await HandleExceptionAsync("Could not allocate participant to service provider from postcode", participantData, basicParticipantCsvRecord.FileName);
                    return;
                }
            }

            // Check if participant has exceptions
            bool ignoreParticipantExceptions = _config.IgnoreParticipantExceptions;
            _logger.LogInformation("Environment variable IgnoreParticipantExceptions is set to {IgnoreParticipantExceptions}", ignoreParticipantExceptions);

            bool participantHasException = participantData.ExceptionFlag == 1;
            if (participantHasException && !ignoreParticipantExceptions) // Will only run if IgnoreParticipantExceptions is false.
            {
                await HandleExceptionAsync($"Unable to add to cohort distribution. As participant with ParticipantId: {participantData.ParticipantId}. Has an Exception against it",
                                                participantData, basicParticipantCsvRecord.FileName);
                return;
            }

            // Validation
            participantData.RecordType = basicParticipantCsvRecord.RecordType;
            var validationResponse = await _CohortDistributionHelper.ValidateCohortDistributionRecordAsync(basicParticipantCsvRecord.FileName, participantData, previousCohortDistributionRecord);

            // Update participant exception flag
            if (validationResponse.CreatedException)
            {
                var errorMessage = $"Participant {participantData.ParticipantId} triggered a validation rule, so will not be added to cohort distribution";
                await HandleExceptionAsync(errorMessage, participantData, basicParticipantCsvRecord.FileName);

                var participantManagement = await _participantManagementClient.GetSingle(participantData.ParticipantId);
                participantManagement.ExceptionFlag = 1;

                bool exceptionFlagUpdated = await _participantManagementClient.Update(participantManagement);
                if (!exceptionFlagUpdated) throw new IOException("Failed to update exception flag");

                if (!ignoreParticipantExceptions) return;
            }
            _logger.LogInformation("Validation has passed or exceptions are ignored, the record with participant id: {ParticipantId} will be added to the database", participantData.ParticipantId);

            // Transformation
            var transformedParticipant = await _CohortDistributionHelper.TransformParticipantAsync(serviceProvider, participantData, previousCohortDistributionRecord);
            if (transformedParticipant == null) return;

            // Add to cohort distribution table
            var cohortAddResponse = await AddCohortDistribution(transformedParticipant);
            if (cohortAddResponse.StatusCode != HttpStatusCode.OK)
            {
                await HandleExceptionAsync("Failed to add the participant to the Cohort Distribution table", transformedParticipant, basicParticipantCsvRecord.FileName);
                return;
            }
            _logger.LogInformation("Participant has been successfully put on the cohort distribution table");
        }
        catch (Exception ex)
        {
            var errorMessage = $"Create Cohort Distribution failed .\nMessage: {ex.Message}\nStack Trace: {ex.StackTrace}";
            await HandleExceptionAsync(errorMessage,
                                    new CohortDistributionParticipant { NhsNumber = basicParticipantCsvRecord.NhsNumber },
                                    basicParticipantCsvRecord.FileName);
            throw;
        }
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
        await _azureQueueStorageHelper.AddItemToQueueAsync<CohortDistributionParticipant>(cohortDistributionParticipant, _config.CohortQueueNamePoison);
    }

    private async Task<HttpResponseMessage> AddCohortDistribution(CohortDistributionParticipant transformedParticipant)
    {
        transformedParticipant.Extracted = DatabaseHelper.ConvertBoolStringToBoolByType("IsExtractedToBSSelect", DataTypes.Integer).ToString();
        var json = JsonSerializer.Serialize(transformedParticipant);
        var response = await _httpClientFunction.SendPost(_config.AddCohortDistributionURL, json);

        _logger.LogInformation("Called {AddCohortDistribution} function", nameof(AddCohortDistribution));
        return response;
    }

    private async Task<CohortDistributionParticipant> GetLatestCohortDistributionRecordAsync(string participantId)
    {
        long longParticipantId = long.Parse(participantId);

        var cohortDistRecords = await _cohortDistributionClient.GetByFilter(x => x.ParticipantId == longParticipantId);
        CohortDistribution? latestParticipant = cohortDistRecords
                                                .OrderByDescending(x => x.CohortDistributionId)
                                                .FirstOrDefault();

        if (latestParticipant != null)
        {
            return new CohortDistributionParticipant(latestParticipant);
        }
        else
        {
            return new CohortDistributionParticipant();
        }
    }
}
