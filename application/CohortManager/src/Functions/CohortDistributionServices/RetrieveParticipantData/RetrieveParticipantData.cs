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
    private readonly ICallFunction _callFunction;
    private readonly ICreateParticipant _createParticipant;
    private readonly IExceptionHandler _exceptionHandler;
    private readonly IDataServiceClient<ParticipantManagement> _participantManagementClient;

    public RetrieveParticipantData(ICreateResponse createResponse, ILogger<RetrieveParticipantData> logger,
                                IDataServiceClient<ParticipantManagement> participantManagementClient,
                                ICreateParticipant createParticipant, IExceptionHandler exceptionHandler,
                                ICallFunction callFunction)
    {
        _createResponse = createResponse;
        _logger = logger;
        _callFunction = callFunction;
        _createParticipant = createParticipant;
        _exceptionHandler = exceptionHandler;
        _participantManagementClient = participantManagementClient;
    }

    [Function("RetrieveParticipantData")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
    {
        long screeningIdLong;
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

            screeningIdLong = long.Parse(requestBody.ScreeningService);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req);
        }

        try
        {
            var longNhsNumber = long.Parse(requestBody.NhsNumber);
            var participantData = await _participantManagementClient.GetSingleByFilter(p => p.NHSNumber == longNhsNumber &&
                                                                            p.ScreeningId == screeningIdLong);
            _logger.LogInformation("Got the participant. ScreeningId: {ScreeningServiceId}", participantData.ScreeningId);

            var demographicFunctionParams = new Dictionary<string, string>()
            {
                {"Id", requestBody.NhsNumber }
            };

            var demographicDataJson = await _callFunction.SendGet(Environment.GetEnvironmentVariable("DemographicDataFunctionURL"), demographicFunctionParams);

            var demographicData = JsonSerializer.Deserialize<Demographic>(demographicDataJson);
            if (demographicData == null)
            {
                _logger.LogError("the demographicData was null the {RetrieveParticipantData}  function", nameof(RetrieveParticipantData));

                return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req, $"the demographicData was null the {nameof(RetrieveParticipantData)}  function");
            }

            participant = _createParticipant.CreateCohortDistributionParticipantModel(participantData, demographicData);
            //TODO, This needs to happen elsewhere Hardcoded for now
            participant.ScreeningName = "Breast Screening";
            participant.ScreeningAcronym = "BSS";

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
