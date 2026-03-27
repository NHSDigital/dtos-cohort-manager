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

    /// <summary>
    /// Processes PDS NotFound responses.
    /// If the NotFound response is part of a NEMS update and the response contains an INVALIDATED_RESOURCE code, the participant will be removed.
    /// </summary>
    /// <param name="pdsResponse"></param>
    /// <param name="nhsNumber"></param>
    /// <returns></returns>
    public async Task ProcessPdsNotFoundResponse(HttpResponseMessage pdsResponse, string nhsNumber, string? sourceFileName = null)
    {
        // Only NEMS updates will have a sourceFileName
        if (string.IsNullOrWhiteSpace(sourceFileName))
        {
            return;
        }

        var errorResponse = await pdsResponse.Content.ReadFromJsonAsync<PdsErrorResponse>();
        if (errorResponse?.issue?.FirstOrDefault()?.details?.coding?.FirstOrDefault()?.code != PdsConstants.InvalidatedResourceCode)
        {
            return;
        }

        _logger.LogInformation("NotFound response contains INVALIDATED_RESOURCE code");

        var pdsDemographic = new PdsDemographic()
        {
            NhsNumber = nhsNumber,
            PrimaryCareProvider = null,
            ReasonForRemoval = PdsConstants.OrrRemovalReason,
            RemovalEffectiveFromDate = DateTime.UtcNow.Date.ToString("yyyyMMdd")
        };
        var participant = new Participant(pdsDemographic);
        participant.RecordType = Actions.Removed;

        await ProcessRecord(participant, sourceFileName);
    }

    /// <summary>
    /// Sends a participant record to the Participant Management service bus topic
    /// </summary>
    /// <param name="participant"></param>
    /// <returns></returns>
    private async Task ProcessRecord(Participant participant, string fileName)
    {
        var updateRecord = new ConcurrentQueue<BasicParticipantCsvRecord>();

        var basicParticipantCsvRecord = new BasicParticipantCsvRecord
        {
            BasicParticipantData = _createBasicParticipantData.BasicParticipantData(participant),
            FileName = fileName,
            Participant = participant
        };

        updateRecord.Enqueue(basicParticipantCsvRecord);

        _logger.LogInformation("Sending record to the Participant Management service bus topic.");
        await _addBatchToQueue.ProcessBatch(updateRecord, _config.ParticipantManagementTopic);
    }

    /// <summary>
    /// adds or updates a demographic record depending on if an record already exists in the database
    /// </summary>
    /// <param name="participantDemographic"></param>
    /// <returns></returns>
    public async Task<bool> UpsertDemographicRecordFromPDS(ParticipantDemographic participantDemographic)
    {
        ParticipantDemographic oldParticipantDemographic = await _participantDemographicClient.GetSingleByFilter(i => i.NhsNumber == participantDemographic.NhsNumber);

        if (oldParticipantDemographic == null)
        {
            _logger.LogInformation("Participant Demographic record not found, attemping to add Participant Demographic.");
            participantDemographic.RecordInsertDateTime = DateTime.UtcNow;
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

        participantDemographic.RecordUpdateDateTime = DateTime.UtcNow;
        participantDemographic.RecordInsertDateTime = oldParticipantDemographic.RecordInsertDateTime;

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
