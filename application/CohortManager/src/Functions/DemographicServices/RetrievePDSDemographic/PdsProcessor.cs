namespace NHS.CohortManager.DemographicServices;

using System.Collections.Concurrent;
using System.Net.Http.Json;
using Common;
using DataServices.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Model;

public class PdsProcessor : IPdsProcessor
{
    private readonly ILogger<PdsProcessor> _logger;

    private readonly IDataServiceClient<ParticipantDemographic> _participantDemographicClient;
    private readonly ICreateBasicParticipantData _createBasicParticipantData;
    private readonly RetrievePDSDemographicConfig _config;
    private readonly IAddBatchToQueue _addBatchToQueue;


    public PdsProcessor(
        ILogger<PdsProcessor> logger,
        ICreateBasicParticipantData createBasicParticipantData,
        IDataServiceClient<ParticipantDemographic> participantDemographicClient,
        IAddBatchToQueue addBatchToQueue,
        IOptions<RetrievePDSDemographicConfig> retrievePDSDemographicConfig)
    {
        _logger = logger;
        _participantDemographicClient = participantDemographicClient;
        _createBasicParticipantData = createBasicParticipantData;
        _addBatchToQueue = addBatchToQueue;
        _config = retrievePDSDemographicConfig.Value;
    }

    public async Task ProcessPdsNotFoundResponse(HttpResponseMessage pdsResponse, string nhsNumber)
    {
        var errorResponse = await pdsResponse!.Content.ReadFromJsonAsync<PdsErrorResponse>();
        // we now create a record as an update record and send to the manage participant function. Reason for removal for date should be today and the reason for remove of ORR
        if (errorResponse!.issue!.FirstOrDefault()!.details!.coding!.FirstOrDefault()!.code == PdsConstants.InvalidatedResourceCode)
        {
            var pdsDemographic = new PdsDemographic()
            {
                NhsNumber = nhsNumber,
                PrimaryCareProvider = null,
                ReasonForRemoval = PdsConstants.OrrRemovalReason,
                RemovalEffectiveFromDate = DateTime.UtcNow.Date.ToString("yyyyMMdd")
            };
            var participant = new Participant(pdsDemographic);
            participant.RecordType = Actions.Removed;
            //sends record for an update
            await ProcessRecord(participant);
            return;
        }
        _logger.LogError("the PDS function has returned a 404 error. function now stopping processing");
    }


    public async Task ProcessRecord(Participant participant)
    {
        var updateRecord = new ConcurrentQueue<BasicParticipantCsvRecord>();
        participant.RecordType = participant.RecordType = Actions.Removed;

        var basicParticipantCsvRecord = new BasicParticipantCsvRecord
        {
            BasicParticipantData = _createBasicParticipantData.BasicParticipantData(participant),
            FileName = PdsConstants.DefaultFileName,
            Participant = participant
        };

        updateRecord.Enqueue(basicParticipantCsvRecord);

        _logger.LogInformation("Sending record to the update queue.");
        await _addBatchToQueue.ProcessBatch(updateRecord, _config.ParticipantManagementTopic);
    }


    public async Task<bool> UpsertDemographicRecordFromPDS(ParticipantDemographic participantDemographic)
    {
        ParticipantDemographic oldParticipantDemographic = await _participantDemographicClient.GetSingleByFilter(i => i.NhsNumber == participantDemographic.NhsNumber);

        if (oldParticipantDemographic == null)
        {
            _logger.LogInformation("Participant Demographic record not found, attemping to add Participant Demographic.");
            bool addSuccess = await _participantDemographicClient.Add(participantDemographic);

            if (addSuccess)
            {
                _logger.LogInformation("Successfully added Participant Demographic.");
                return true;
            }

            _logger.LogError("Failed to add Participant Demographic.");
            return false;
        }

        _logger.LogInformation("Participant Demographic record found, attempting to update Participant Demographic.");
        participantDemographic.ParticipantId = oldParticipantDemographic.ParticipantId;
        bool updateSuccess = await _participantDemographicClient.Update(participantDemographic);

        if (updateSuccess)
        {
            _logger.LogInformation("Successfully updated Participant Demographic.");
            return true;
        }

        _logger.LogError("Failed to update Participant Demographic.");
        return false;
    }

}