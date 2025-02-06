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
using DataServices.Client;

public class RetrieveParticipantData
{
    private readonly ICreateResponse _createResponse;
    private readonly ILogger<RetrieveParticipantData> _logger;
    private readonly ICreateDemographicData _createDemographicData;
    private readonly ICreateParticipant _createParticipant;
    private readonly IExceptionHandler _exceptionHandler;
    private readonly IDataServiceClient<ParticipantManagement> _participantManagementClient;

    public RetrieveParticipantData(ICreateResponse createResponse, ILogger<RetrieveParticipantData> logger,
                                ICreateDemographicData createDemographicData, ICreateParticipant createParticipant,
                                IExceptionHandler exceptionHandler, IDataServiceClient<ParticipantManagement> participantManagementClient)
    {
        _createResponse = createResponse;
        _logger = logger;
        _createDemographicData = createDemographicData;
        _createParticipant = createParticipant;
        _exceptionHandler = exceptionHandler;
        _participantManagementClient = participantManagementClient;
    }

    [Function("RetrieveParticipantData")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
    {
        RetrieveParticipantRequestBody requestBody;
        var participant = new CohortDistributionParticipant();
        try
        {
            string requestBodyJson;
            using (var reader = new StreamReader(req.Body, Encoding.UTF8))
            {
                requestBodyJson = await reader.ReadToEndAsync();
            }
            requestBody = JsonSerializer.Deserialize<RetrieveParticipantRequestBody>(requestBodyJson);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req);
        }

        try
        {
            var participantData = await _participantManagementClient.GetSingleByFilter(p => p.NHSNumber == long.Parse(requestBody.NhsNumber) &&
                                                                            p.ScreeningId == long.Parse(requestBody.ScreeningService));
            _logger.LogInformation("Got the participant. ScreeningId: {ScreeningServiceId}", participantData.ScreeningId);

            var demographicData = _createDemographicData.GetDemographicData(requestBody.NhsNumber);
            participant = _createParticipant.CreateCohortDistributionParticipantModel(participantData, demographicData);
            var responseBody = JsonSerializer.Serialize(participant);
            _logger.LogInformation("ParticipantScreeningID: {ScreeningServiceId}", participant.ScreeningServiceId);

            return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req, responseBody);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Retrieve participant data failed.\nMessage: {Message}\nStack Trace: {StackTrace}", ex.Message, ex.StackTrace);
            await _exceptionHandler.CreateSystemExceptionLogFromNhsNumber(ex, requestBody.NhsNumber, "", "", JsonSerializer.Serialize(participant) ?? "N/A");
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
        }
    }
}
