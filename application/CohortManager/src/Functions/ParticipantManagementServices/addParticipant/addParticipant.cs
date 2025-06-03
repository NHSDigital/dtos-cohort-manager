/// <summary>
/// Takes a participant from the queue, gets data from the demographic service,
/// validates the participant, then calls create participant, mark as eligible, and create cohort distribution
/// </summary>

namespace addParticipant;

using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Common;
using Model;
using NHS.Screening.AddParticipant;
using Microsoft.Extensions.Options;

public class AddParticipantFunction
{
    private readonly ILogger<AddParticipantFunction> _logger;
    private readonly IHttpClientFunction _httpClientFunction;
    private readonly ICheckDemographic _getDemographicData;
    private readonly ICreateParticipant _createParticipant;
    private readonly IExceptionHandler _handleException;
    private readonly ICohortDistributionHandler _cohortDistributionHandler;
    private readonly AddParticipantConfig _config;

    public AddParticipantFunction(
        ILogger<AddParticipantFunction> logger,
        IHttpClientFunction httpClientFunction,
        ICheckDemographic checkDemographic,
        ICreateParticipant createParticipant,
        IExceptionHandler handleException,
        ICohortDistributionHandler cohortDistributionHandler,
        IOptions<AddParticipantConfig> addParticipantConfig
        )
    {
        _logger = logger;
        _httpClientFunction = httpClientFunction;
        _getDemographicData = checkDemographic;
        _createParticipant = createParticipant;
        _handleException = handleException;
        _cohortDistributionHandler = cohortDistributionHandler;
        _config = addParticipantConfig.Value;
    }

    [Function(nameof(AddParticipantFunction))]
    public async Task Run([QueueTrigger("%AddQueueName%", Connection = "AzureWebJobsStorage")] string jsonFromQueue)
    {
        _logger.LogInformation("C# addParticipant called.");

        var basicParticipantCsvRecord = JsonSerializer.Deserialize<BasicParticipantCsvRecord>(jsonFromQueue);

        try
        {
            // Get demographic data
            var demographicData = await _getDemographicData.GetDemographicAsync(basicParticipantCsvRecord.Participant.NhsNumber, _config.DemographicURIGet);
            if (demographicData == null)
            {
                _logger.LogInformation("demographic function failed");
                await _handleException.CreateSystemExceptionLog(new Exception("demographic function failed"), basicParticipantCsvRecord.Participant, basicParticipantCsvRecord.FileName);
                return;
            }

            var participant = _createParticipant.CreateResponseParticipantModel(basicParticipantCsvRecord.Participant, demographicData);
            var participantCsvRecord = new ParticipantCsvRecord
            {
                Participant = participant,
                FileName = basicParticipantCsvRecord.FileName,
            };

            // Validation
            participantCsvRecord.Participant.ExceptionFlag = "N";
            participant.ExceptionFlag = "N";
            var response = await ValidateData(participantCsvRecord);
            if (response.IsFatal)
            {
                _logger.LogError("A fatal Rule was violated, so the record cannot be added to the database");
                await _handleException.CreateSystemExceptionLog(null, basicParticipantCsvRecord.Participant, basicParticipantCsvRecord.FileName);
                return;
            }

            if (response.CreatedException)
            {
                participantCsvRecord.Participant.ExceptionFlag = "Y";
                participant.ExceptionFlag = "Y";
            }

            // Add participant to database
            var json = JsonSerializer.Serialize(participantCsvRecord);
            var createResponse = await _httpClientFunction.SendPost(_config.DSaddParticipant, json);

            if (createResponse.StatusCode != HttpStatusCode.OK)
            {
                _logger.LogError("There was problem posting the participant to the database");
                await _handleException.CreateSystemExceptionLog(new Exception("There was problem posting the participant to the database"), basicParticipantCsvRecord.Participant, basicParticipantCsvRecord.FileName);
                return;

            }
            _logger.LogInformation("Participant created");

            // Send to cohort distribution
            var cohortDistResponse = await _cohortDistributionHandler.SendToCohortDistributionService(participant.NhsNumber, participant.ScreeningId, participant.RecordType, basicParticipantCsvRecord.FileName, participant);
            if (!cohortDistResponse)
            {
                _logger.LogError("Participant failed to send to Cohort Distribution Service");
                await _handleException.CreateSystemExceptionLog(new Exception("Participant failed to send to Cohort Distribution Service"), basicParticipantCsvRecord.Participant, basicParticipantCsvRecord.FileName);
                return;
            }

        }
        catch (Exception ex)
        {
            _logger.LogInformation(ex, "Unable to call function.\nMessage: {Message}\nStack Trace: {StackTrace}", ex.Message, ex.StackTrace);
            await _handleException.CreateSystemExceptionLog(ex, basicParticipantCsvRecord.Participant, basicParticipantCsvRecord.FileName);
        }
    }

    private async Task<ValidationExceptionLog> ValidateData(ParticipantCsvRecord participantCsvRecord)
    {
        var json = JsonSerializer.Serialize(participantCsvRecord);

        if (string.IsNullOrWhiteSpace(participantCsvRecord.Participant.ScreeningName))
        {
            var errorDescription = $"A record with Nhs Number: {participantCsvRecord.Participant.NhsNumber} has invalid screening name and therefore cannot be processed by the static validation function";
            await _handleException.CreateRecordValidationExceptionLog(participantCsvRecord.Participant.NhsNumber, participantCsvRecord.FileName, errorDescription, "", JsonSerializer.Serialize(participantCsvRecord.Participant));

            return new ValidationExceptionLog()
            {
                IsFatal = false,
                CreatedException = true
            };
        }

        var response = await _httpClientFunction.SendPost(_config.StaticValidationURL, json);
        var responseBodyJson = await _httpClientFunction.GetResponseText(response);
        var responseBody = JsonSerializer.Deserialize<ValidationExceptionLog>(responseBodyJson);

        return responseBody;

    }
}
