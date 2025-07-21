namespace updateParticipantDetails;

using System.Net;
using System.Text;
using System.Text.Json;
using Common;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Model;
using DataServices.Client;
using NHS.Screening.UpdateParticipantDetails;
using Microsoft.Extensions.Options;

public class UpdateParticipantDetails
{
    private readonly ILogger<UpdateParticipantDetails> _logger;
    private readonly ICreateResponse _createResponse;
    private readonly IExceptionHandler _handleException;
    private readonly IHttpClientFunction _httpClientFunction;
    private readonly IDataServiceClient<ParticipantManagement> _participantManagementClient;
    private readonly UpdateParticipantDetailsConfig _config;

    public UpdateParticipantDetails(ILogger<UpdateParticipantDetails> logger, ICreateResponse createResponse,
                                    IExceptionHandler handleException, IHttpClientFunction httpClientFunction,
                                    IDataServiceClient<ParticipantManagement> participantManagementClient,
                                    IOptions<UpdateParticipantDetailsConfig> updateParticipantDetailsConfig)
    {
        _logger = logger;
        _createResponse = createResponse;
        _handleException = handleException;
        _httpClientFunction = httpClientFunction;
        _participantManagementClient = participantManagementClient;
        _config = updateParticipantDetailsConfig.Value;
    }

    [Function("updateParticipantDetails")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
    {
        var participantCsvRecord = new ParticipantCsvRecord();
        try
        {
            string requestBody = "";

            using (StreamReader reader = new StreamReader(req.Body, Encoding.UTF8))
            {
                requestBody = await reader.ReadToEndAsync();
                participantCsvRecord = JsonSerializer.Deserialize<ParticipantCsvRecord>(requestBody);
            }

            Participant reqParticipant = participantCsvRecord.Participant;


            long nhsNumberLong;
            if (!long.TryParse(reqParticipant.NhsNumber, out nhsNumberLong))
            {
                throw new FormatException("Could not parse Long in update participant details");
            }

            long ScreeningIdLong;
            if (!long.TryParse(reqParticipant.ScreeningId, out ScreeningIdLong))
            {
                throw new FormatException("Could not parse Long in update participant details");
            }

            var existingParticipantData = await _participantManagementClient.GetSingleByFilter(p => p.NHSNumber == nhsNumberLong
                                                                                        && p.ScreeningId == ScreeningIdLong);

            var response = await ValidateData(new Participant(existingParticipantData), participantCsvRecord.Participant, participantCsvRecord.FileName);
            if (response.IsFatal)
            {
                _logger.LogError("Validation Error: A fatal Rule was violated and therefore the record cannot be added to the database with Nhs number: {ParticipantId}", participantCsvRecord.Participant.ParticipantId);
                return _createResponse.CreateHttpResponse(HttpStatusCode.Created, req);
            }

            if (response.CreatedException)
            {
                _logger.LogInformation("Validation Error: A Rule was violated but it was not Fatal for record with Participant Id: {ParticipantId}", participantCsvRecord.Participant.ParticipantId);
                reqParticipant.ExceptionFlag = "1";
            }

            reqParticipant.ParticipantId = existingParticipantData.ParticipantId.ToString();
            reqParticipant.RecordUpdateDateTime = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
            var ParticipantManagementRecord = reqParticipant.ToParticipantManagement();

            //Mark Participant as Eligible/Ineligible
            ParticipantManagementRecord.EligibilityFlag = (short)(reqParticipant.EligibilityFlag == EligibilityFlag.Eligible ? 1 : 0);

            var isAdded = await _participantManagementClient.Update(ParticipantManagementRecord);

            if (isAdded)
                return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req);

            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message, ex);
            await _handleException.CreateSystemExceptionLog(ex, participantCsvRecord.Participant, participantCsvRecord.FileName);
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
            _logger.LogInformation(ex, "Lookup validation failed.\nMessage: {Message}\nParticipant: REDACTED", ex.Message);
            return null;
        }
    }
}
