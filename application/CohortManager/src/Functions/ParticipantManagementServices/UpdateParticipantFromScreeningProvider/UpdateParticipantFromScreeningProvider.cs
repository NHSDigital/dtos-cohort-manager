namespace NHS.CohortManager.ParticipantManagementServices;

using Microsoft.Azure.Functions.Worker;
using Azure.Messaging.EventGrid;
using Microsoft.Extensions.Logging;
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

    public UpdateParticipantFromScreeningProvider(ILogger<UpdateParticipantFromScreeningProvider> logger,
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

    /// <summary>
    /// Updates the participant managment table given a request from a screening provider (through
    /// the BI & Data Analytics product). Sends an event to Event Grid if the update is successful.
    /// </summary>
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
            _logger.LogError(ex, "Request participant is invalid");
            await _exceptionHandler.CreateSystemExceptionLogFromNhsNumber(ex, reqParticipant.NhsNumber.ToString(), "", "BSS", eventGridEvent.Data.ToString());
            return;
        }
        try
        {
            ParticipantManagement dbParticipant = await _participantManagementClient.GetSingleByFilter(p => p.NHSNumber == reqParticipant.NhsNumber
                                                                                                        && p.ScreeningId == reqParticipant.ScreeningId);
            if (dbParticipant == null)
            {
                _logger.LogError("Participant update failed, participant could not be found");
                await _exceptionHandler.CreateSystemExceptionLogFromNhsNumber(new KeyNotFoundException("Update participant from screening provider failed, could not find participant"),
                                                                            reqParticipant.NhsNumber.ToString(), "", "BSS", eventGridEvent.Data.ToString());
                return;
            }

            // Replace Gene Code & Higher Risk Reason Code with relevant foreign key
            long? higherRiskReasonPk = null;
            long? geneCodePk= null;

            if (reqParticipant.GeneCode != null)
            {
                GeneCodeLkp geneCode = await _geneCodeClient.GetSingleByFilter(x => x.GeneCode == reqParticipant.GeneCode);
                geneCodePk = geneCode.GeneCodeId;
            }

            if (reqParticipant.HigherRiskReferralReasonCode != null)
            {
                HigherRiskReferralReasonLkp higherRiskReason = await _higherRiskReferralReasonClient
                    .GetSingleByFilter(x => x.HigherRiskReferralReasonCode == reqParticipant.HigherRiskReferralReasonCode);
                higherRiskReasonPk = higherRiskReason.HigherRiskReferralReasonId;
            }

            var participantManagement = reqParticipant.ToParticipantManagement(dbParticipant, geneCodePk, higherRiskReasonPk);

            // Update data (only when the request data is newer)
            bool updated = false;
            if (dbParticipant.SrcSysProcessedDateTime != null && dbParticipant.SrcSysProcessedDateTime > reqParticipant.SrcSysProcessedDateTime)
                updated = true;
            else
            {
                participantManagement.ParticipantId = dbParticipant.ParticipantId;
                updated = await _participantManagementClient.Update(participantManagement);
            }

            if (!updated)
            {
                _logger.LogError("Failed to update participant management table");
                await _exceptionHandler.CreateSystemExceptionLogFromNhsNumber(new IOException("Failed to update participant management table"),
                                                                            reqParticipant.NhsNumber.ToString(), "", "BSS", eventGridEvent.Data.ToString());
                return;
            }

            var message = new EventGridEvent(
                subject: "ParticipantUpdate",
                eventType: "NSP.ParticipantUpdateReceived",
                dataVersion: "1.0",
                data: reqParticipant
            );

            var result = await _eventGridPublisherClient.SendEventAsync(message);

            if (result.Status != 200)
            {
                _logger.LogError("Failed to send event");
                await _exceptionHandler.CreateSystemExceptionLogFromNhsNumber(new IOException("Failed to send event"),
                                                                            reqParticipant.NhsNumber.ToString(), "", "BSS", eventGridEvent.Data.ToString());
                return;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update participant failed.");
            await _exceptionHandler.CreateSystemExceptionLogFromNhsNumber(ex, reqParticipant.NhsNumber.ToString(), "", "BSS", eventGridEvent.Data.ToString());
        }
    }
}

