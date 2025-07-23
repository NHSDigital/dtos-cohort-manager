namespace NHS.CohortManager.ScreeningDataServices;

using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Common;
using Model;
using DataServices.Client;
using NHS.Screening.CreateParticipant;

public class CreateParticipant
{
    private readonly ILogger<CreateParticipant> _logger;
    private readonly ICreateResponse _createResponse;
    private readonly IExceptionHandler _handleException;
    private readonly IDataServiceClient<ParticipantManagement> _participantManagementClient;
    private readonly IHttpClientFunction _httpClientFunction;
    private readonly CreateParticipantConfig _config;

    public CreateParticipant(
        ILogger<CreateParticipant> logger,
        ICreateResponse createResponse,
        IExceptionHandler handleException,
        IHttpClientFunction httpClientFunction,
        IDataServiceClient<ParticipantManagement> participantManagementClient,
        IOptions<CreateParticipantConfig> createParticipantConfig)
    {
        _logger = logger;
        _createResponse = createResponse;
        _handleException = handleException;
        _httpClientFunction = httpClientFunction;
        _participantManagementClient = participantManagementClient;
        _config = createParticipantConfig.Value;
    }

    [Function("CreateParticipant")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
    {
        ParticipantCsvRecord participantCsvRecord = null;
        var existingParticipant = new Participant();
        long screeningId;
        long nhsNumber;
        try
        {
            using (StreamReader reader = new StreamReader(req.Body, Encoding.UTF8))
            {
                var requestBody = await reader.ReadToEndAsync();
                participantCsvRecord = JsonSerializer.Deserialize<ParticipantCsvRecord>(requestBody);
            }

            if (!long.TryParse(participantCsvRecord.Participant.ScreeningId, out screeningId))
                throw new FormatException("Could not parse ScreeningId");

            if (!long.TryParse(participantCsvRecord.Participant.NhsNumber, out nhsNumber))
                throw new FormatException("Could not parse NhsNumber");

            var existingParticipantResult = await _participantManagementClient.GetByFilter(i => i.NHSNumber == nhsNumber && i.ScreeningId == screeningId);

            if (existingParticipantResult != null && existingParticipantResult.Any())
                existingParticipant = new Participant(existingParticipantResult.First());

            var response = await ValidateData(existingParticipant, participantCsvRecord.Participant, participantCsvRecord.FileName);
            if (response.IsFatal)
            {
                _logger.LogError("Validation Error: A fatal Rule was violated and therefore the record cannot be added to the database with Nhs number: REDACTED");
                return _createResponse.CreateHttpResponse(HttpStatusCode.Created, req);
            }

            if (response.CreatedException)
                participantCsvRecord.Participant.ExceptionFlag = "Y";

            var ParticipantManagementRecord = participantCsvRecord.Participant.ToParticipantManagement();
            if (!short.TryParse(participantCsvRecord.Participant.EligibilityFlag, out var eligibilityFlagFromParticipant))
            {
                _logger.LogError("Could not parse eligibility flag '{EligibilityFlag}' for participant",
                    participantCsvRecord.Participant.EligibilityFlag);

                return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req, "could not parse eligibility flag from participant in file");
            }
            ParticipantManagementRecord.EligibilityFlag = eligibilityFlagFromParticipant;
            var participantCreated = await _participantManagementClient.Add(ParticipantManagementRecord);

            if (participantCreated)
            {
                _logger.LogInformation("Successfully created the participant");
                return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req);
            }
            _logger.LogError("Failed to create the participant");
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to make the CreateParticipant request\nMessage: {Message}", ex.Message);
            await _handleException.CreateSystemExceptionLog(ex, participantCsvRecord?.Participant, participantCsvRecord?.FileName);
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
        }
    }

    private async Task<ValidationExceptionLog> ValidateData(Participant existingParticipant, Participant newParticipant, string fileName)
    {
        var json = JsonSerializer.Serialize(new LookupValidationRequestBody(existingParticipant, newParticipant, fileName));

        try
        {
            var response = await _httpClientFunction.SendPost(_config.LookupValidationURL, json);
            var responseBodyJson = await _httpClientFunction.GetResponseText(response);
            var responseBody = JsonSerializer.Deserialize<ValidationExceptionLog>(responseBodyJson);

            return responseBody;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lookup validation failed.\nMessage: {Message}\nParticipant: REDACTED", ex.Message);
            throw new HttpRequestException("Lookup validation failed", ex);
        }
    }
}
