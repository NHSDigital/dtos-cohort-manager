namespace NHS.CohortManager.ParticipantManagementServices;

using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Azure.Messaging.EventGrid;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using System.Text;
using Model;
using System.Text.Json;
using Common;
using DataServices.Client;

public class UpdateParticipantFromScreeningProvider
{
    private readonly ILogger<UpdateParticipantFromScreeningProvider> _logger;
    private readonly IDataServiceClient<ParticipantManagement> _participantManagementClient;
    private readonly IDataServiceClient<HigherRiskReferralReasonLkp> _higherRiskReferralReasonClient;
    private readonly IDataServiceClient<GeneCodeLkp> _geneCodeClient;
    private BiAnalyticsParticipantDto reqParticipant;
    private readonly ICreateResponse _createResponse;
    private readonly ICallFunction _callFunction;

    public UpdateParticipantFromScreeningProvider(ILogger<UpdateParticipantFromScreeningProvider> logger,
                                                ICreateResponse createResponse, ICallFunction callFunction,
                                                IDataServiceClient<ParticipantManagement> participantManagementClient,
                                                IDataServiceClient<HigherRiskReferralReasonLkp> higherRiskReferralReasonClient,
                                                IDataServiceClient<GeneCodeLkp> geneCodeClient)
    {
        _logger = logger;
        _createResponse = createResponse;
        _callFunction = callFunction;
        _participantManagementClient = participantManagementClient;
        _higherRiskReferralReasonClient =  higherRiskReferralReasonClient;
        _geneCodeClient = geneCodeClient;
    }

    // TODO: change to eventgrid trigger
    [Function("UpdateParticipantFromScreeningProvider")]
    public async Task<HttpResponseData> Run([EventGridTrigger] EventGridEvent eventGridEvent)
    {   
        _logger.LogInformation("Update participant from screening provider called.");

        try
        {
            string requestBodyJson;
            using (var reader = new StreamReader(req.Body, Encoding.UTF8))
            {
                requestBodyJson = reader.ReadToEnd();
            }

            reqParticipant = JsonSerializer.Deserialize<BiAnalyticsParticipantDto>(requestBodyJson);
        }
        catch (Exception ex)
        {
            _logger.LogError("Request participant is invalid: {Ex}", ex);
            return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req);
        }

        try
        {
            var dbParticipants = await _participantManagementClient.GetByFilter(p => p.NHSNumber == reqParticipant.NhsNumber);

            // Participant does not exist in the DB
            if (dbParticipants == null || !dbParticipants.Any())
            {
                _logger.LogError("Participant not found");
                return await _createResponse.CreateHttpResponseWithBodyAsync(HttpStatusCode.NotFound, req, "Participant not found");
            }

            ParticipantManagement dbParticipant = dbParticipants.FirstOrDefault();

            // Database contains more recent data
            if (dbParticipant.RecordUpdateDateTime > reqParticipant.SrcSysProcessedDateTime)
            {
                _logger.LogInformation("Request participant data is older than database participant data");
                return await _createResponse.CreateHttpResponseWithBodyAsync(HttpStatusCode.InternalServerError, req, "Participant not found");
            }


            // Replace Gene Code & Higher Risk Reason Code with relevant foreign key
            var geneCodes = await _geneCodeClient.GetByFilter(x => x.GeneCode == reqParticipant.GeneCode);
            long geneCodePk = geneCodes.FirstOrDefault().GeneCodeId;

            var higherRiskReasons = await _higherRiskReferralReasonClient.GetByFilter(x => x.HigherRiskReferralReasonCode == reqParticipant.HigherRiskReferralReasonCode);
            long higherRiskReasonPk = higherRiskReasons.FirstOrDefault().HigherRiskReferralReasonId;

            var participantManagement = reqParticipant.ToParticipantManagement(geneCodePk, higherRiskReasonPk, dbParticipant);

            // update data
            participantManagement.ParticipantId = dbParticipant.ParticipantId;
            bool updated = await _participantManagementClient.Update(participantManagement);

            if (!updated) throw new IOException("Updating participant managment object failed");

            return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req);
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex.Message);
            return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req);
        }
        catch (Exception ex)
        {
            _logger.LogInformation($"Update participant failed.\nMessage: {ex.Message}\nStack Trace: {ex.StackTrace}");
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
        }
    }
}

