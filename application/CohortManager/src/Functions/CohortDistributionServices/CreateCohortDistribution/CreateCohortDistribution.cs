namespace NHS.CohortManager.CohortDistributionService;

using Microsoft.Azure.Functions.Worker;
using Common;
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
    private readonly ICohortDistributionHelper _CohortDistributionHelper;
    private readonly IExceptionHandler _exceptionHandler;
    private readonly IQueueClient _azureQueueStorageHelper;
    private readonly IDataServiceClient<ParticipantManagement> _participantManagementClient;
    private readonly CreateCohortDistributionConfig _config;
    private readonly IDataServiceClient<CohortDistribution> _cohortDistributionClient;
    private const string DefaultNhsNumber = "0";

    public CreateCohortDistribution(ILogger<CreateCohortDistribution> logger,
                                    ICohortDistributionHelper CohortDistributionHelper,
                                    IExceptionHandler exceptionHandler,
                                    IQueueClient azureQueueStorageHelper,
                                    IDataServiceClient<ParticipantManagement> participantManagementClient,
                                    IDataServiceClient<CohortDistribution> cohortDistributionClient,
                                    IOptions<CreateCohortDistributionConfig> createCohortDistributionConfig)
    {
        _logger = logger;
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
        if (!IsValidRequest(basicParticipantCsvRecord))
        {
            await HandleExceptionAsync("One or more of the required parameters is missing.", null, basicParticipantCsvRecord.FileName);
            return;
        }

        try
        {
            await ProcessCohortDistributionAsync(basicParticipantCsvRecord);
        }
        catch (Exception ex)
        {
            var errorMessage = $"Create Cohort Distribution failed .\nMessage: {ex.Message}\nStack Trace: {ex.StackTrace}";
            await HandleExceptionAsync(errorMessage,
                                    new CohortDistributionParticipant { NhsNumber = basicParticipantCsvRecord.NhsNumber },
                                    basicParticipantCsvRecord.FileName!);
            throw;
        }
    }

    private static bool IsValidRequest(CreateCohortDistributionRequestBody request)
    {
        return !string.IsNullOrWhiteSpace(request.ScreeningService) &&
               !string.IsNullOrWhiteSpace(request.NhsNumber);
    }

    private async Task ProcessCohortDistributionAsync(CreateCohortDistributionRequestBody basicParticipantCsvRecord)
    {
        var participantData = await RetrieveAndValidateParticipantDataAsync(basicParticipantCsvRecord);
        if (participantData == null) return;

        var previousCohortDistributionRecord = await GetLatestCohortDistributionRecordAsync(participantData.ParticipantId);

        var serviceProvider = await AllocateServiceProviderAsync(basicParticipantCsvRecord, participantData);
        if (serviceProvider == null) return;

        var shouldContinue = await HandleParticipantExceptionsAsync(participantData, basicParticipantCsvRecord.FileName!);
        if (!shouldContinue) return;

        participantData.RecordType = basicParticipantCsvRecord.RecordType;
        var shouldProceed = await ValidateAndHandleExceptionsAsync(basicParticipantCsvRecord.FileName!, participantData, previousCohortDistributionRecord);
        if (!shouldProceed) return;

        await TransformAndAddParticipantAsync(serviceProvider, participantData, previousCohortDistributionRecord, basicParticipantCsvRecord.FileName);
    }

    private async Task<CohortDistributionParticipant> RetrieveAndValidateParticipantDataAsync(CreateCohortDistributionRequestBody basicParticipantCsvRecord)
    {
        var participantData = await _CohortDistributionHelper.RetrieveParticipantDataAsync(basicParticipantCsvRecord);

        if (participantData == null || string.IsNullOrEmpty(participantData.ScreeningServiceId))
        {
            await HandleExceptionAsync("Participant data returned from database is missing required fields", participantData, basicParticipantCsvRecord.FileName);
        }

        return participantData;
    }

    private async Task<string?> AllocateServiceProviderAsync(CreateCohortDistributionRequestBody basicParticipantCsvRecord, CohortDistributionParticipant participantData)
    {
        var serviceProvider = EnumHelper.GetDisplayName(ServiceProvider.BSS);

        if (string.IsNullOrEmpty(participantData.Postcode))
        {
            return serviceProvider;
        }

        serviceProvider = await _CohortDistributionHelper.AllocateServiceProviderAsync(
            basicParticipantCsvRecord.NhsNumber,
            participantData.ScreeningAcronym,
            participantData.Postcode,
            JsonSerializer.Serialize(participantData)
        );

        if (serviceProvider == null)
        {
            await HandleExceptionAsync("Could not allocate Participant to service provider from postcode", participantData, basicParticipantCsvRecord.FileName);
            return serviceProvider;
        }

        return serviceProvider;
    }

    private async Task<bool> HandleParticipantExceptionsAsync(CohortDistributionParticipant participantData, string fileName)
    {
        var ignoreParticipantExceptions = _config.IgnoreParticipantExceptions;
        _logger.LogInformation("Environment variable IgnoreParticipantExceptions is set to {IgnoreParticipantExceptions}", ignoreParticipantExceptions);

        var participantHasException = participantData.ExceptionFlag == 1;

        if (participantHasException && !ignoreParticipantExceptions)
        {
            await HandleExceptionAsync($"Unable to add to Cohort Distribution. As Participant with ParticipantId: {participantData.ParticipantId}. Has an Exception against it", participantData, fileName);
            return false;
        }

        return true;
    }

    private async Task<bool> ValidateAndHandleExceptionsAsync(string fileName, CohortDistributionParticipant participantData, CohortDistributionParticipant previousCohortDistributionRecord)
    {
        var validationResponse = await _CohortDistributionHelper.ValidateCohortDistributionRecordAsync(fileName, participantData, previousCohortDistributionRecord);

        if (!validationResponse.CreatedException)
        {
            _logger.LogInformation("Validation has passed or exceptions are ignored, the record with Participant id: {ParticipantId} will be added to the database", participantData.ParticipantId);
            return true;
        }

        var errorMessage = $"Participant {participantData.ParticipantId} triggered a validation rule, so will not be added to Cohort Distribution";
        await HandleExceptionAsync(errorMessage, participantData, fileName);

        await UpdateParticipantExceptionFlagAsync(participantData.ParticipantId);

        var ignoreParticipantExceptions = _config.IgnoreParticipantExceptions;
        if (ignoreParticipantExceptions)
        {
            _logger.LogInformation("Validation has passed or exceptions are ignored, the record with Participant id: {ParticipantId} will be added to the database", participantData.ParticipantId);
            return true;
        }

        return false;
    }

    private async Task UpdateParticipantExceptionFlagAsync(string participantId)
    {
        var participantManagement = await _participantManagementClient.GetSingle(participantId);
        participantManagement.ExceptionFlag = 1;

        var exceptionFlagUpdated = await _participantManagementClient.Update(participantManagement);
        if (!exceptionFlagUpdated)
        {
            throw new IOException("Failed to update exception flag");
        }
    }

    private async Task TransformAndAddParticipantAsync(string serviceProvider, CohortDistributionParticipant participantData, CohortDistributionParticipant previousCohortDistributionRecord, string fileName)
    {
        var transformedParticipant = await _CohortDistributionHelper.TransformParticipantAsync(serviceProvider, participantData, previousCohortDistributionRecord);
        if (transformedParticipant == null)
        {
            return;
        }

        var participantAdded = await AddCohortDistribution(transformedParticipant);
        if (!participantAdded)
        {
            await HandleExceptionAsync("Failed to add the Participant to the Cohort Distribution table", transformedParticipant, fileName);
            return;
        }

        _logger.LogInformation("Participant has been successfully put on the Cohort Distribution table");
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

    private async Task<bool> AddCohortDistribution(CohortDistributionParticipant transformedParticipant)
    {
        transformedParticipant.Extracted = DatabaseHelper.ConvertBoolStringToBoolByType("IsExtractedToBSSelect", DataTypes.Integer).ToString();
        var cohortDistributionParticipantToAdd = transformedParticipant.ToCohortDistribution();
        var isAdded = await _cohortDistributionClient.Add(cohortDistributionParticipantToAdd);

        _logger.LogInformation("sent Participant to Cohort Distribution data service");
        return isAdded;
    }

    private async Task<CohortDistributionParticipant> GetLatestCohortDistributionRecordAsync(string participantId)
    {
        long longParticipantId = long.Parse(participantId);

        var cohortDistRecords = await _cohortDistributionClient.GetByFilter(x => x.ParticipantId == longParticipantId);
        var latestParticipant = cohortDistRecords.OrderByDescending(x => x.CohortDistributionId).FirstOrDefault();

        return latestParticipant != null
            ? new CohortDistributionParticipant(latestParticipant)
            : new CohortDistributionParticipant { NhsNumber = DefaultNhsNumber };
    }
}
