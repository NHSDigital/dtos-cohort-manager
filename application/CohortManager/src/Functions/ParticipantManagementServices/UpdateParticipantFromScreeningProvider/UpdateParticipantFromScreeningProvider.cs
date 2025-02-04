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
    private BiAnalyticsParticipantDto _reqParticipant;
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
            _reqParticipant = JsonSerializer.Deserialize<BiAnalyticsParticipantDto>(eventGridEvent.Data.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Request participant is invalid");
            await _exceptionHandler.CreateSystemExceptionLogFromNhsNumber(ex, _reqParticipant.NhsNumber.ToString(), "", "BSS", eventGridEvent.Data.ToString());
            return;
        }
        try
        {
            ParticipantManagement dbParticipant = await _participantManagementClient.GetSingleByFilter(p => p.NHSNumber == _reqParticipant.NhsNumber
                                                                                                        && p.ScreeningId == _reqParticipant.ScreeningId);
            if (dbParticipant == null)
            {
                _logger.LogError("Participant update failed, participant could not be found");
                await _exceptionHandler.CreateSystemExceptionLogFromNhsNumber(new KeyNotFoundException("Could not find participant"),
                                                                            _reqParticipant.NhsNumber.ToString(), "", "BSS", eventGridEvent.Data.ToString());
                return;
            }

            var participantManagement = _reqParticipant.ToParticipantManagement(dbParticipant);

            // Replace Gene Code & Higher Risk Reason Code with relevant foreign key
            GeneCodeLkp? geneCode = null;
            HigherRiskReferralReasonLkp? higherRiskReason = null;

            if (_reqParticipant.GeneCode != null)
            {
                geneCode = await _geneCodeClient.GetSingleByFilter(x => x.GeneCode == _reqParticipant.GeneCode);
                participantManagement.GeneCodeId = geneCode.GeneCodeId;
            }

            if (_reqParticipant.HigherRiskReferralReasonCode != null)
            {
                higherRiskReason = await _higherRiskReferralReasonClient
                    .GetSingleByFilter(x => x.HigherRiskReferralReasonCode == _reqParticipant.HigherRiskReferralReasonCode);
                participantManagement.HigherRiskReferralReasonId = higherRiskReason.HigherRiskReferralReasonId;
            }

            // Update data (only when the request data is newer)
            bool updateSuccessful = true;
            bool reqDataNewer = true;
            if (dbParticipant.SrcSysProcessedDateTime != null && dbParticipant.SrcSysProcessedDateTime > _reqParticipant.SrcSysProcessedDateTime)
            {
                reqDataNewer = false;
            }
            else
            {
                participantManagement.ParticipantId = dbParticipant.ParticipantId;
                updateSuccessful = await _participantManagementClient.Update(participantManagement);
            }

            if (reqDataNewer && !updateSuccessful)
            {
                _logger.LogError("Failed to update participant management table");
                await _exceptionHandler.CreateSystemExceptionLogFromNhsNumber(new IOException("Failed to update participant management table"),
                                                                            _reqParticipant.NhsNumber.ToString(), "", "BSS", eventGridEvent.Data.ToString());
                return;
            }

            UpdateFromScreeningProviderReturnModel returnParticipant = new(_reqParticipant, geneCode, higherRiskReason)
            {
                ReasonForRemoval = dbParticipant.ReasonForRemoval,
                ReasonForRemovalDate = dbParticipant.ReasonForRemovalDate
            };

            var message = new EventGridEvent(
                subject: "ParticipantUpdate",
                eventType: "NSP.ParticipantUpdateReceived",
                dataVersion: "1.0",
                data: returnParticipant
            );

            var result = await _eventGridPublisherClient.SendEventAsync(message);

            if (result.Status != 200)
            {
                _logger.LogError("Failed to send event to Event Grid");
                await _exceptionHandler.CreateSystemExceptionLogFromNhsNumber(new IOException("Failed to send event to Event Grid"),
                                                                            _reqParticipant.NhsNumber.ToString(), "", "BSS", eventGridEvent.Data.ToString());
                return;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update participant failed.");
            await _exceptionHandler.CreateSystemExceptionLogFromNhsNumber(ex, _reqParticipant.NhsNumber.ToString(), "", "BSS", eventGridEvent.Data.ToString());
        }
    }
}

