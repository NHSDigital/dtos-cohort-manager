namespace NHS.CohortManager.CohortDistributionService;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Common;
using System.Net;
using Microsoft.Extensions.Logging;
using Model;
using NHS.CohortManager.CohortDistribution;
using System.Text;
using System.Text.Json;
using Data.Database;

public class RetrieveParticipantData
{
    private readonly ICreateResponse _createResponse;
    private readonly ILogger<RetrieveParticipantData> _logger;
    private readonly IParticipantManagerData _participantManagerData;
    private readonly ICreateDemographicData _createDemographicData;
    private readonly ICreateParticipant _createParticipant;

    private readonly IExceptionHandler _exceptionHandler;

    public RetrieveParticipantData(ICreateResponse createResponse, ILogger<RetrieveParticipantData> logger, IParticipantManagerData participantManagerData, ICreateDemographicData createDemographicData, ICreateParticipant createParticipant, IExceptionHandler exceptionHandler)
    {
        _createResponse = createResponse;
        _logger = logger;
        _participantManagerData = participantManagerData;
        _createDemographicData = createDemographicData;
        _createParticipant = createParticipant;
    }

    [Function("RetrieveParticipantData")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
    {
        RetrieveParticipantRequestBody requestBody;
        try
        {
            string requestBodyJson;
            using (var reader = new StreamReader(req.Body, Encoding.UTF8))
            {
                requestBodyJson = reader.ReadToEnd();
            }
            requestBody = JsonSerializer.Deserialize<RetrieveParticipantRequestBody>(requestBodyJson);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
        }

        try
        {
            var participantData = _participantManagerData.GetParticipantFromIDAndScreeningService(requestBody);
            var demographicData = _createDemographicData.GetDemographicData(requestBody.NhsNumber);
            var participant = _createParticipant.CreateCohortDistributionParticipantModel(participantData, demographicData);

            var responseBody = JsonSerializer.Serialize(participant);

            return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req, responseBody);
        }
        catch (Exception ex)
        {

            _logger.LogError("Retrieve participant data failed.\nMessage: {Message}\nStack Trace: {StackTrace}", ex.Message, ex.StackTrace);
            await _exceptionHandler.CreateSystemExceptionLogFromNhsNumber(ex, requestBody.NhsNumber, "");
            return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req);
        }
    }
}
