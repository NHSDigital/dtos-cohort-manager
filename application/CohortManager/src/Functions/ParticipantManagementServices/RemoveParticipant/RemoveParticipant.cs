namespace NHS.CohortManager.ParticipantManagementService;

using System.Net;
using System.Text;
using System.Text.Json;
using Common;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Model;
using NHS.Screening.RemoveParticipant;

public class RemoveParticipant
{
    private readonly ILogger<RemoveParticipant> _logger;
    private readonly ICreateResponse _createResponse;
    private readonly IHttpClientFunction _httpClientFunction;
    private readonly IExceptionHandler _handleException;
    private readonly ICohortDistributionHandler _cohortDistributionHandler;
    private readonly RemoveParticipantConfig _config;

    public RemoveParticipant(
        ILogger<RemoveParticipant> logger,
        ICreateResponse createResponse,
        IHttpClientFunction httpClientFunction,
        IExceptionHandler handleException,
        ICohortDistributionHandler cohortDistributionHandler,
        IOptions<RemoveParticipantConfig> removeParticipantConfig)
    {
        _logger = logger;
        _createResponse = createResponse;
        _httpClientFunction = httpClientFunction;
        _handleException = handleException;
        _cohortDistributionHandler = cohortDistributionHandler;
        _config = removeParticipantConfig.Value;
    }

    [Function("RemoveParticipant")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
    {
        BasicParticipantCsvRecord basicParticipantCsvRecord = new BasicParticipantCsvRecord();
        try
        {
            using (var reader = new StreamReader(req.Body, Encoding.UTF8))
            {
                var requestBodyJson = await reader.ReadToEndAsync();
                basicParticipantCsvRecord = JsonSerializer.Deserialize<BasicParticipantCsvRecord>(requestBodyJson);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            await _handleException.CreateSystemExceptionLog(ex, basicParticipantCsvRecord.BasicParticipantData, basicParticipantCsvRecord.FileName);
            return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req);
        }

        try
        {
            var participantCsvRecord = new ParticipantCsvRecord
            {
                Participant = basicParticipantCsvRecord.participant,
                FileName = basicParticipantCsvRecord.FileName,
            };
            participantCsvRecord.Participant.EligibilityFlag = EligibilityFlag.Ineligible; //Mark Participant As Ineligible
            var ineligibleResponse = await UpdateParticipant(participantCsvRecord);

            if (!ineligibleResponse)
            {
                _logger.LogInformation("Marking Participant As Ineligible request has failed.");
                return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
            }

            var cohortDistributionResponse = await SendToCohortDistribution(basicParticipantCsvRecord.participant, participantCsvRecord.FileName);

            if (!cohortDistributionResponse)
            {
                _logger.LogInformation("SendToCohortDistribution request has failed.");
                return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
            }

            _logger.LogInformation("Participant successfully removed.");
            return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to call function.\nMessage: {Message}\nStack Trace: {StackTrace}", ex.Message, ex.StackTrace);
            await _handleException.CreateSystemExceptionLog(ex, basicParticipantCsvRecord.participant, basicParticipantCsvRecord.FileName);
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
        }
    }

    private async Task<bool> SendToCohortDistribution(Participant participant, string fileName)
    {
        if (!await _cohortDistributionHandler.SendToCohortDistributionService(participant.NhsNumber, participant.ScreeningId, participant.RecordType, fileName, participant))
        {
            return false;
        }
        return true;
    }

    private async Task<bool> UpdateParticipant(ParticipantCsvRecord participantCsvRecord)
    {
        var json = JsonSerializer.Serialize(participantCsvRecord);

        var createResponse = await _httpClientFunction.SendPost(_config.UpdateParticipant, json);
        if (createResponse.StatusCode == HttpStatusCode.OK)
        {
            _logger.LogInformation("Participant updated as ineligible.");
            return true;
        }
        return false;
    }
}
