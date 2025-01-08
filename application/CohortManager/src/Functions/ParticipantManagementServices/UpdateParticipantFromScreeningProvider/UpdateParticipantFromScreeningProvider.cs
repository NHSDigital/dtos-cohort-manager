namespace NHS.CohortManager.ParticipantManagementServices;

using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Azure.Messaging.EventGrid;
using Microsoft.Extensions.Logging;
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
    private readonly EventGridPublisherClient _eventGridPublisherClient;
    private readonly IExceptionHandler _exceptionHandler;

    public UpdateParticipantFromScreeningProvider(ILogger<UpdateParticipantFromScreeningProvider> logger,,
                                                IDataServiceClient<ParticipantManagement> participantManagementClient,
                                                IDataServiceClient<HigherRiskReferralReasonLkp> higherRiskReferralReasonClient,
                                                IDataServiceClient<GeneCodeLkp> geneCodeClient,
                                                EventGridPublisherClient eventGridPublisherClient,
                                                IExceptionHandler exceptionHandler)
    {
        _logger = logger;
        _participantManagementClient = participantManagementClient;
        _higherRiskReferralReasonClient =  higherRiskReferralReasonClient;
        _geneCodeClient = geneCodeClient;
        _eventGridPublisherClient = eventGridPublisherClient;
        _exceptionHandler = exceptionHandler;
    }

    // Default URL for triggering event grid function in the local environment.
    // http://localhost:7071/runtime/webhooks/EventGrid?functionName={functionname}
    // TODO: change to eventgrid trigger
    [Function("UpdateParticipantFromScreeningProvider")]
    public async Task Run([EventGridTrigger] EventGridEvent eventGridEvent)
    {   
        _logger.LogInformation("Update participant from screening provider called.");

        try
        {
            reqParticipant = JsonSerializer.Deserialize<BiAnalyticsParticipantDto>(eventGridEvent.Data.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError("Request participant is invalid: {Ex}", ex);
            await _exceptionHandler.CreateSystemExceptionLogFromNhsNumber(ex, reqParticipant.NhsNumber.ToString(), "", "BSS", eventGridEvent.Data.ToString());
            return;
        }

        try
        {
            var dbParticipants = await _participantManagementClient.GetByFilter(p => p.NHSNumber == reqParticipant.NhsNumber);

            // Participant does not exist in the DB
            if (dbParticipants == null || !dbParticipants.Any())
                throw new KeyNotFoundException("Participant not found");

            ParticipantManagement dbParticipant = dbParticipants.FirstOrDefault();

            // Database contains more recent data
            if (dbParticipant.RecordUpdateDateTime > reqParticipant.SrcSysProcessedDateTime)
                throw new InvalidOperationException("Request participant data is older than database participant data");

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

            EventGridEvent message = new EventGridEvent(
                subject: "IDK",
                eventType: "IDK",
                dataVersion: "1.0",
                data: reqParticipant
            );

            var result = await _eventGridPublisherClient.SendEventAsync(eventGridEvent);

            return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Update participant failed.\nMessage: {ex.Message}\nStack Trace: {ex.StackTrace}");
            await _exceptionHandler.CreateSystemExceptionLogFromNhsNumber(ex, reqParticipant.NhsNumber.ToString(), "", "BSS", eventGridEvent.Data.ToString());
            return;
        }
    }
}

