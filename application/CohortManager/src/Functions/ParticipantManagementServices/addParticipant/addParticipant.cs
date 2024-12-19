/// <summary>
/// Takes a participant from the queue, gets data from the demographic service,
/// validates the participant, then calls create participant, mark as eligible, and create cohort distribution
/// </summary>

namespace addParticipant;

using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Common;
using Model;

public class AddParticipantFunction
{
    private readonly ILogger<AddParticipantFunction> _logger;
    private readonly ICallFunction _callFunction;
    private readonly ICreateResponse _createResponse;
    private readonly ICheckDemographic _getDemographicData;
    private readonly ICreateParticipant _createParticipant;
    private readonly IExceptionHandler _handleException;
    private readonly ICohortDistributionHandler _cohortDistributionHandler;

    public AddParticipantFunction(ILogger<AddParticipantFunction> logger, ICallFunction callFunction, ICreateResponse createResponse, ICheckDemographic checkDemographic, ICreateParticipant createParticipant, IExceptionHandler handleException, ICohortDistributionHandler cohortDistributionHandler)
    {
        _logger = logger;
        _callFunction = callFunction;
        _createResponse = createResponse;
        _getDemographicData = checkDemographic;
        _createParticipant = createParticipant;
        _handleException = handleException;
        _cohortDistributionHandler = cohortDistributionHandler;
    }

    [Function(nameof(AddParticipantFunction))]
    public async Task Run([QueueTrigger("%AddQueueName%", Connection = "AzureWebJobsStorage")] string jsonFromQueue)
    {

        _logger.LogInformation("C# addParticipant called.");
        HttpWebResponse createResponse, eligibleResponse;

        _logger.LogDebug("Starting detailed deserialization of the queue message for processing.");
        _logger.LogInformation("Deserializing queue message...");
        var basicParticipantCsvRecord = JsonSerializer.Deserialize<BasicParticipantCsvRecord>(jsonFromQueue);

        try
        {
        _logger.LogDebug("Starting retrieval of detailed demographic data for participant.");
         _logger.LogInformation("Retrieving demographic data for participant...");
        var demographicData = await _getDemographicData.GetDemographicAsync(basicParticipantCsvRecord.Participant.NhsNumber, Environment.GetEnvironmentVariable("DemographicURIGet"));

        if (demographicData == null)
            {
             _logger.LogWarning("Demographic data retrieval returned null.");
             _logger.LogDebug("Detailed trace: Demographic function execution failed. This might be due to invalid input or API timeout.");
             _logger.LogInformation("demographic function failed");
             await _handleException.CreateSystemExceptionLog(new Exception("demographic function failed"), basicParticipantCsvRecord.Participant, basicParticipantCsvRecord.FileName);
             return;
            }
            _logger.LogDebug("Demographic data successfully retrieved.");

            _logger.LogDebug("Initializing participant model creation with detailed attributes.");
            _logger.LogInformation("Creating participant model...");
            var participant = _createParticipant.CreateResponseParticipantModel(basicParticipantCsvRecord.Participant, demographicData);
            var participantCsvRecord = new ParticipantCsvRecord
            {
                Participant = participant,
                FileName = basicParticipantCsvRecord.FileName,
            };

            // Validation
            participantCsvRecord.Participant.ExceptionFlag = "N";
            _logger.LogDebug("Participant model successfully instantiated with all required properties set.");
            _logger.LogInformation("Participant model created successfully.");

            _logger.LogDebug("Initiating in-depth validation of participant data with all attributes and constraints.");
            _logger.LogInformation("Validating participant data...");
            var response = await ValidateData(participantCsvRecord);
            if (response.IsFatal)
            {
                _logger.LogError("A fatal Rule was violated, so the record cannot be added to the database");
                await _handleException.CreateSystemExceptionLog(null, basicParticipantCsvRecord.Participant, basicParticipantCsvRecord.FileName);
                return;
            }

            if (response.CreatedException)
            {
                _logger.LogWarning("Validation created an exception. Setting ExceptionFlag to 'Y'.");
                participantCsvRecord.Participant.ExceptionFlag = "Y";
            }
            _logger.LogDebug("Detailed validation checks on participant data completed without any errors or warnings.");
             _logger.LogInformation("Participant data validation completed successfully.");

            _logger.LogDebug("Preparing payload and initiating API request to Create Participant endpoint.");
            _logger.LogInformation("Sending participant data to Create Participant API...");
            var json = JsonSerializer.Serialize(participantCsvRecord);
            createResponse = await _callFunction.SendPost(Environment.GetEnvironmentVariable("DSaddParticipant"), json);

            if (createResponse.StatusCode != HttpStatusCode.OK)
            {
                _logger.LogError("There was problem posting the participant to the database");
                await _handleException.CreateSystemExceptionLog(new Exception("There was problem posting the participant to the database"), basicParticipantCsvRecord.Participant, basicParticipantCsvRecord.FileName);
                return;

            }
            _logger.LogDebug("Participant successfully created with all attributes populated.");
            _logger.LogInformation("participant created");


            _logger.LogDebug("Preparing and sending detailed participant payload to Eligibility API for validation.");
            _logger.LogInformation("Sending participant data to Eligibility API...");
            var participantJson = JsonSerializer.Serialize(participant);
            eligibleResponse = await _callFunction.SendPost(Environment.GetEnvironmentVariable("DSmarkParticipantAsEligible"), participantJson);

            if (eligibleResponse.StatusCode != HttpStatusCode.OK)
            {
                await _handleException.CreateSystemExceptionLog(new Exception("There was an error while marking participant as eligible {eligibleResponse}"), basicParticipantCsvRecord.Participant, basicParticipantCsvRecord.FileName);
                return;
            }
            _logger.LogInformation("participant created, marked as eligible at  {datetime}", DateTime.UtcNow);

            _logger.LogDebug("Initiating cohort distribution with participant details and screening ID.");
            _logger.LogInformation("Distributing participant to cohort...");
            _logger.LogDebug("Adding participant data to cohort tool at {datetime} for detailed tracking.", DateTime.UtcNow);
            _logger.LogInformation("adding to cohort tool {datetime}", DateTime.UtcNow);
            if (!await _cohortDistributionHandler.SendToCohortDistributionService(participant.NhsNumber, participant.ScreeningId, participant.RecordType, basicParticipantCsvRecord.FileName, participant))
            {
                _logger.LogError("participant failed to send to Cohort Distribution Service");
                await _handleException.CreateSystemExceptionLog(new Exception("participant failed to send to Cohort Distribution Service"), basicParticipantCsvRecord.Participant, basicParticipantCsvRecord.FileName);
                return;

            }

            _logger.LogDebug("Participant successfully distributed to cohort with all required parameters validated.");
            _logger.LogInformation("Participant successfully distributed to cohort.");
            _logger.LogDebug("Initiated API call to Cohort Distribution Service with payload at {datetime}.", DateTime.UtcNow);
            _logger.LogInformation("participant sent to Cohort Distribution Service at {datetime}", DateTime.UtcNow);

        }
        catch (Exception ex)
        {
            _logger.LogInformation(ex, $"Unable to call function.\nMessage: {ex.Message}\nStack Trace: {ex.StackTrace}");
            await _handleException.CreateSystemExceptionLog(ex, basicParticipantCsvRecord.Participant, basicParticipantCsvRecord.FileName);
            return;
        }
    }

    private async Task<ValidationExceptionLog> ValidateData(ParticipantCsvRecord participantCsvRecord)
    {
        _logger.LogDebug("Serializing participant data for validation.");
        var json = JsonSerializer.Serialize(participantCsvRecord);

        try
        {
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

            var response = await _callFunction.SendPost(Environment.GetEnvironmentVariable("StaticValidationURL"), json);
            var responseBodyJson = await _callFunction.GetResponseText(response);
            var responseBody = JsonSerializer.Deserialize<ValidationExceptionLog>(responseBodyJson);

            return responseBody;
        }
        catch (Exception ex)
        {
            _logger.LogInformation($"Static validation failed.\nMessage: {ex.Message}\nParticipant: {participantCsvRecord}");
            return null;
        }
    }
}
