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
    /// the BI & Data Analytics product).
    /// </summary>
    /// <returns>An Event Grid event</returns>
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
            ParticipantManagement dbParticipant = await _participantManagementClient.GetSingleByFilter(p => p.NHSNumber == reqParticipant.NhsNumber)
            ?? throw new KeyNotFoundException("Participant not found");

            // Database contains more recent data
            if (dbParticipant.SrcSysProcessedDateTime != null && dbParticipant.SrcSysProcessedDateTime > reqParticipant.SrcSysProcessedDateTime)
                throw new InvalidOperationException("Request participant data is older than database participant data");

            // Replace Gene Code & Higher Risk Reason Code with relevant foreign key
            GeneCodeLkp geneCode = await _geneCodeClient.GetSingleByFilter(x => x.GeneCode == reqParticipant.GeneCode);
            long geneCodePk = geneCode.GeneCodeId;

            HigherRiskReferralReasonLkp higherRiskReason = await _higherRiskReferralReasonClient.GetSingleByFilter(x => x.HigherRiskReferralReasonCode == reqParticipant.HigherRiskReferralReasonCode);
            long higherRiskReasonPk = higherRiskReason.HigherRiskReferralReasonId;

            var participantManagement = reqParticipant.ToParticipantManagement(geneCodePk, higherRiskReasonPk, dbParticipant);

            // update data
            participantManagement.ParticipantId = dbParticipant.ParticipantId;
            bool updated = await _participantManagementClient.Update(participantManagement);

            if (!updated) throw new IOException("Updating participant management object failed");

            var message = new EventGridEvent(
                subject: "IDK",
                eventType: "Success",
                dataVersion: "1.0",
                data: reqParticipant
            );

            var result = await _eventGridPublisherClient.SendEventAsync(message);

            if (result.Status != 200) throw new IOException("Sending event failed");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Update participant failed.\nMessage: {ex.Message}\nStack Trace: {ex.StackTrace}");
            await _exceptionHandler.CreateSystemExceptionLogFromNhsNumber(ex, reqParticipant.NhsNumber.ToString(), "", "BSS", eventGridEvent.Data.ToString());
        }
    }
}

