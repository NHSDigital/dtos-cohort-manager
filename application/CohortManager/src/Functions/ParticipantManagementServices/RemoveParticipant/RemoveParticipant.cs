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
using DataServices.Client;
using NHS.Screening.RemoveParticipant;

public class RemoveParticipant
{
    private readonly ILogger<RemoveParticipant> _logger;
    private readonly ICreateResponse _createResponse;
    private readonly IHttpClientFunction _httpClientFunction;
    private readonly IExceptionHandler _handleException;
    private readonly ICohortDistributionHandler _cohortDistributionHandler;
    private readonly RemoveParticipantConfig _config;
    private readonly IDataServiceClient<ParticipantManagement> _participantManagementClient;

    public RemoveParticipant(
        ILogger<RemoveParticipant> logger,
        ICreateResponse createResponse,
        IHttpClientFunction httpClientFunction,
        IExceptionHandler handleException,
        ICohortDistributionHandler cohortDistributionHandler,
        IOptions<RemoveParticipantConfig> removeParticipantConfig,
        IDataServiceClient<ParticipantManagement> participantManagementClient)
    {
        _logger = logger;
        _createResponse = createResponse;
        _httpClientFunction = httpClientFunction;
        _handleException = handleException;
        _cohortDistributionHandler = cohortDistributionHandler;
        _config = removeParticipantConfig.Value;
        _participantManagementClient = participantManagementClient;
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
            await _handleException.CreateSystemExceptionLog(ex, basicParticipantCsvRecord.Participant, basicParticipantCsvRecord.FileName);
            return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req);
        }

        try
        {
            var participantCsvRecord = new ParticipantCsvRecord
            {
                Participant = basicParticipantCsvRecord.participant,
                FileName = basicParticipantCsvRecord.FileName,
            };

            var ineligibleResponse = await MarkParticipantAsIneligible(participantCsvRecord);

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

    private async Task<bool> MarkParticipantAsIneligible(ParticipantCsvRecord participantCsvRecord)
    {
        long nhsNumber;
        long screeningId;
        var updated = false;
        if (participantCsvRecord.Participant != null)
        {
            if (!long.TryParse(participantCsvRecord.Participant.NhsNumber, out nhsNumber))
            {
                throw new FormatException("Could not parse NhsNumber");
            }
            if (!long.TryParse(participantCsvRecord.Participant.ScreeningId, out screeningId))
            {
                throw new FormatException("Could not parse ScreeningId");
            }

            var participantManagementRecord = await _participantManagementClient.GetSingleByFilter(x => x.NHSNumber == nhsNumber && x.ScreeningId == screeningId);
            participantManagementRecord.EligibilityFlag = 0;

            updated = await _participantManagementClient.Update(participantManagementRecord);
        }

        if (!updated)
        {
            return false;
        }

        _logger.LogInformation("Successfully marked participant as Ineligible for NHS Number: REDACTED}");
        return true;
    }
}
