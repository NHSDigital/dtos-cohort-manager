namespace NHS.CohortManager.CohortDistributionService;

using Microsoft.Azure.Functions.Worker;
using Common;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NHS.CohortManager.CohortDistribution;
using System.Text;
using Model;
using Model.Enums;
using Data.Database;

public class CreateCohortDistribution
{
    private readonly ICreateResponse _createResponse;
    private readonly ILogger<CreateCohortDistribution> _logger;
    private readonly ICallFunction _callFunction;
    private readonly ICohortDistributionHelper _CohortDistributionHelper;
    private readonly IExceptionHandler _exceptionHandler;
    private readonly IParticipantManagerData _participantManagerData;

    private readonly IAzureQueueStorageHelper _azureQueueStorageHelper;

    public CreateCohortDistribution(ICreateResponse createResponse, ILogger<CreateCohortDistribution> logger, ICallFunction callFunction, ICohortDistributionHelper CohortDistributionHelper, IExceptionHandler exceptionHandler, IParticipantManagerData participantManagerData, IAzureQueueStorageHelper azureQueueStorageHelper)
    {
        _createResponse = createResponse;
        _logger = logger;
        _callFunction = callFunction;
        _CohortDistributionHelper = CohortDistributionHelper;
        _exceptionHandler = exceptionHandler;
        _participantManagerData = participantManagerData;
        _azureQueueStorageHelper = azureQueueStorageHelper;
    }

    [Function(nameof(CreateCohortDistribution))]
    public async Task RunAsync([QueueTrigger("%CohortQueueName%", Connection = "AzureWebJobsStorage")] CreateCohortDistributionRequestBody basicParticipantCsvRecord)
    {
        if (string.IsNullOrWhiteSpace(basicParticipantCsvRecord.ScreeningService) || string.IsNullOrWhiteSpace(basicParticipantCsvRecord.NhsNumber))
        {
            string logMessage = $"One or more of the required parameters is missing.";
            _logger.LogError(logMessage);
            await HandleErrorResponseAsync(logMessage, null, basicParticipantCsvRecord.FileName);
            return;
        }

        try
        {
            // Retrieve participant data
            var participantData = await _CohortDistributionHelper.RetrieveParticipantDataAsync(basicParticipantCsvRecord);
            if (participantData == null)
            {
                _logger.LogInformation("participant data in cohort distribution was null");
                await HandleErrorResponseAsync("There was a problem getting participant data in cohort distribution", participantData, basicParticipantCsvRecord.FileName);
                return;
            }
            _logger.LogInformation("participant data Screening Id: {participantData}", participantData.ScreeningServiceId);

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

            if (ParticipantHasException(basicParticipantCsvRecord.NhsNumber, participantData.ScreeningServiceId))
            {
                _logger.LogInformation($"Unable to add to cohort distribution. As participant with ParticipantId: {participantData.ParticipantId}. Has an Exception against it");
                await HandleErrorResponseAsync($"Unable to add to cohort distribution. As participant with ParticipantId: {participantData.ParticipantId}. Has an Exception against it",
                                                participantData, basicParticipantCsvRecord.FileName);

                return;
            }

            // Validate cohort distribution record & transform data service
            participantData.RecordType = basicParticipantCsvRecord.RecordType;
            var validationRecordCreated = await _CohortDistributionHelper.ValidateCohortDistributionRecordAsync(basicParticipantCsvRecord.NhsNumber, basicParticipantCsvRecord.FileName, participantData);
            if (!validationRecordCreated)
            {
                _logger.LogInformation("Validation has passed the record with NHS number: {NhsNumber} will be added to the database", participantData.NhsNumber);

                var transformedParticipant = await _CohortDistributionHelper.TransformParticipantAsync(serviceProvider, participantData);

                if (transformedParticipant == null)
                {
                    _logger.LogError("The transform participant returned null in cohort distribution");
                    await HandleErrorResponseAsync("the transformed participant returned null from the transform participant function", transformedParticipant, basicParticipantCsvRecord.FileName);
                    return;
                }

                var cohortAddResponse = await AddCohortDistribution(transformedParticipant);
                if (cohortAddResponse.StatusCode != HttpStatusCode.OK)
                {
                    await HandleErrorResponseAsync("the transformed participant returned null from the transform participant function", transformedParticipant, basicParticipantCsvRecord.FileName);
                    return;
                }
                _logger.LogInformation("participant has been successfully put on the cohort distribution table");
                return;
            }
            var errorMessage = $"Validation error: A rule triggered a fatal error, preventing the cohort distribution record with participant Id {participantData.ParticipantId} from being added to the database";
            _logger.LogInformation(errorMessage);
            await _exceptionHandler.CreateRecordValidationExceptionLog(participantData.NhsNumber, basicParticipantCsvRecord.FileName, errorMessage, serviceProvider, JsonSerializer.Serialize(participantData));


        }
        catch (Exception ex)
        {
            _logger.LogError("One of the functions failed.\nMessage: {Message}\nStack Trace: {StackTrace}", ex.Message, ex.StackTrace);
            await _exceptionHandler.CreateSystemExceptionLogFromNhsNumber(ex, basicParticipantCsvRecord.NhsNumber, basicParticipantCsvRecord.FileName, "", JsonSerializer.Serialize(basicParticipantCsvRecord.ErrorRecord) ?? "N/A");
            throw;
        }
    }

    private async Task HandleErrorResponseAsync(string errorMessage, CohortDistributionParticipant cohortDistributionParticipant, string fileName)
    {
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
        var json = JsonSerializer.Serialize(transformedParticipant);
        var response = await _callFunction.SendPost(Environment.GetEnvironmentVariable("AddCohortDistributionURL"), json);

        _logger.LogInformation("Called add cohort distribution function");
        return response;
    }

    private bool ParticipantHasException(string nhsNumber, string screeningId)
    {
        var participant = _participantManagerData.GetParticipant(nhsNumber, screeningId);
        var exceptionFlag = Enum.TryParse(participant.ExceptionFlag, out Exists value) ? value : Exists.No;
        return exceptionFlag == Exists.Yes;
    }
}
