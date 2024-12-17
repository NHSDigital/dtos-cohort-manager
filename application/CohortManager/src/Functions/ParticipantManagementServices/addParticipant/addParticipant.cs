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
        _logger.LogInformation("Starting processing of queue message...");

        _logger.LogInformation("C# addParticipant called.");
        HttpWebResponse createResponse, eligibleResponse;

        _logger.LogInformation("Deserializing queue message...");
        var basicParticipantCsvRecord = JsonSerializer.Deserialize<BasicParticipantCsvRecord>(jsonFromQueue);


        try
        {
         _logger.LogInformation("Retrieving demographic data for participant...");
        var demographicData = await _getDemographicData.GetDemographicAsync(basicParticipantCsvRecord.Participant.NhsNumber, Environment.GetEnvironmentVariable("DemographicURIGet"));

        if (demographicData == null)
            {
             _logger.LogWarning("Demographic data retrieval returned null.");
             _logger.LogInformation("demographic function failed");
             await _handleException.CreateSystemExceptionLog(new Exception("demographic function failed"), basicParticipantCsvRecord.Participant, basicParticipantCsvRecord.FileName);
             return;
            }
            _logger.LogInformation("Demographic data successfully retrieved.");


            _logger.LogInformation("Creating participant model...");
            var participant = _createParticipant.CreateResponseParticipantModel(basicParticipantCsvRecord.Participant, demographicData);
            var participantCsvRecord = new ParticipantCsvRecord
            {
                Participant = participant,
                FileName = basicParticipantCsvRecord.FileName,
            };
            participantCsvRecord.Participant.ExceptionFlag = "N";
            _logger.LogInformation("Participant model created successfully.");

            _logger.LogInformation("Validating participant data...");
            var response = await ValidateData(participantCsvRecord);
            if (response.IsFatal)
            {
                _logger.LogError("A fatal Rule was violated and therefore the record cannot be added to the database");
                await _handleException.CreateSystemExceptionLog(null, basicParticipantCsvRecord.Participant, basicParticipantCsvRecord.FileName);
                return;
            }

            if (response.CreatedException)
            {
                _logger.LogWarning("Validation created an exception. Setting ExceptionFlag to 'Y'.");
                participantCsvRecord.Participant.ExceptionFlag = "Y";
            }
             _logger.LogInformation("Participant data validation completed successfully.");


            _logger.LogInformation("Sending participant data to Create Participant API...");
            var json = JsonSerializer.Serialize(participantCsvRecord);
            _logger.LogInformation("ADD: sending record to add at {datetime}", DateTime.UtcNow);
            createResponse = await _callFunction.SendPost(Environment.GetEnvironmentVariable("DSaddParticipant"), json);

            if (createResponse.StatusCode != HttpStatusCode.OK)
            {
                _logger.LogError("There was problem posting the participant to the database");
                await _handleException.CreateSystemExceptionLog(new Exception("There was problem posting the participant to the database"), basicParticipantCsvRecord.Participant, basicParticipantCsvRecord.FileName);
                return;

            }
            _logger.LogInformation("participant created");

            _logger.LogInformation("Sending participant data to Eligibility API...");
            var participantJson = JsonSerializer.Serialize(participant);
            _logger.LogInformation("Eligible: sending record to add at {datetime}", DateTime.UtcNow);
            eligibleResponse = await _callFunction.SendPost(Environment.GetEnvironmentVariable("DSmarkParticipantAsEligible"), participantJson);
            _logger.LogInformation("Response Eligible: sending record to add at {datetime}", DateTime.UtcNow);

            if (eligibleResponse.StatusCode != HttpStatusCode.OK)
            {
                _logger.LogError($"There was an error while marking participant as eligible {eligibleResponse}");
                await _handleException.CreateSystemExceptionLog(new Exception("There was an error while marking participant as eligible {eligibleResponse}"), basicParticipantCsvRecord.Participant, basicParticipantCsvRecord.FileName);
                return;
            }
            _logger.LogInformation("participant created, marked as eligible at  {datetime}", DateTime.UtcNow);


            _logger.LogInformation("Distributing participant to cohort...");
            _logger.LogInformation("adding to cohort tool {datetime}", DateTime.UtcNow);
            if (!await _cohortDistributionHandler.SendToCohortDistributionService(participant.NhsNumber, participant.ScreeningId, participant.RecordType, basicParticipantCsvRecord.FileName, participant))
            {
                _logger.LogError("participant failed to send to Cohort Distribution Service");
                await _handleException.CreateSystemExceptionLog(new Exception("participant failed to send to Cohort Distribution Service"), basicParticipantCsvRecord.Participant, basicParticipantCsvRecord.FileName);
                return;

            }

            _logger.LogInformation("Participant successfully distributed to cohort.");
            _logger.LogInformation("participant sent to Cohort Distribution Service at {datetime}", DateTime.UtcNow);

        }
        catch (Exception ex)
        {
            _logger.LogInformation(ex, $"Unable to call function.\nMessage: {ex.Message}\nStack Trace: {ex.StackTrace}");
            await _handleException.CreateSystemExceptionLog(ex, basicParticipantCsvRecord.Participant, basicParticipantCsvRecord.FileName);
        }
    }

    private async Task<ValidationExceptionLog> ValidateData(ParticipantCsvRecord participantCsvRecord)
    {
        _logger.LogInformation("[1] Serializing participant data for validation step 1...");
        _logger.LogInformation("[1] Participant validation in progress - Step 1...");
        _logger.LogInformation("[1] Preparing participant data for validation - Log 1...");
        _logger.LogInformation("[1] Participant data validation started for record 1...");
        _logger.LogInformation("[1] Initiating validation of participant data - Attempt 1...");
        _logger.LogInformation("[2] Serializing participant data for validation step 2...");
        _logger.LogInformation("[2] Participant validation in progress - Step 2...");
        _logger.LogInformation("[2] Preparing participant data for validation - Log 2...");
        _logger.LogInformation("[2] Participant data validation started for record 2...");
        _logger.LogInformation("[2] Initiating validation of participant data - Attempt 2...");
        _logger.LogInformation("[3] Serializing participant data for validation step 3...");
        _logger.LogInformation("[3] Participant validation in progress - Step 3...");
        _logger.LogInformation("[3] Preparing participant data for validation - Log 3...");
        _logger.LogInformation("[3] Participant data validation started for record 3...");
        _logger.LogInformation("[3] Initiating validation of participant data - Attempt 3...");

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
