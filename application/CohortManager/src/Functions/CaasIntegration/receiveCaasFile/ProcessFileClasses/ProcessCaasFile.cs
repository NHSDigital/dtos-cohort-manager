namespace NHS.Screening.ReceiveCaasFile;

using System.Text.Json;
using Common;
using Common.Interfaces;
using Data.Database;
using DataServices.Client;
using Hl7.Fhir.Rest;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.Configuration;
using Model;
using Model.Enums;

public class ProcessCaasFile : IProcessCaasFile
{
    private readonly ILogger<ProcessCaasFile> _logger;
    private readonly IReceiveCaasFileHelper _receiveCaasFileHelper;
    private readonly ICallDurableDemographicFunc _callDurableDemographicFunc;
    private readonly IAddBatchToQueue _addBatchToQueue;
    private readonly IExceptionHandler _exceptionHandler;
    private readonly IDataServiceClient<ParticipantDemographic> _participantDemographic;
    private readonly IRecordsProcessedTracker _recordsProcessTracker;
    private readonly IValidateDates _validateDates;
    private readonly ReceiveCaasFileConfig _config;
    private readonly string DemographicURI;


    public ProcessCaasFile(
        ILogger<ProcessCaasFile> logger,
        IAddBatchToQueue addBatchToQueue,
        IReceiveCaasFileHelper receiveCaasFileHelper,
        IExceptionHandler exceptionHandler,
        IDataServiceClient<ParticipantDemographic> participantDemographic,
        IRecordsProcessedTracker recordsProcessedTracker,
        IValidateDates validateDates,
        ICallDurableDemographicFunc callDurableDemographicFunc,
        IOptions<ReceiveCaasFileConfig> receiveCaasFileConfig
    )
    {
        _logger = logger;
        _addBatchToQueue = addBatchToQueue;
        _receiveCaasFileHelper = receiveCaasFileHelper;
        _exceptionHandler = exceptionHandler;
        _participantDemographic = participantDemographic;
        _recordsProcessTracker = recordsProcessedTracker;
        _validateDates = validateDates;
        _callDurableDemographicFunc = callDurableDemographicFunc;
        _config = receiveCaasFileConfig.Value;
        DemographicURI = _config.DemographicURI;
    }

    /// <summary>
    /// process a given batch and send it the queue
    /// </summary>
    /// <param name="values"></param>
    /// <param name="options"></param>
    /// <param name="screeningService"></param>
    /// <param name="filename"></param>
    /// <returns></returns>
    public async Task ProcessRecords(List<ParticipantsParquetMap> values, ParallelOptions options, ScreeningLkp screeningService, string filename)
    {
        var currentBatch = new Batch();
        await Parallel.ForEachAsync(values, options, async (rec, cancellationToken) =>
        {
            var participant = _receiveCaasFileHelper.MapParticipant(rec, screeningService.ScreeningId.ToString(), screeningService.ScreeningName, filename);

            if (participant == null)
            {
                await _exceptionHandler.CreateSystemExceptionLogFromNhsNumber(new Exception($"Could not map participant in file {filename}"), rec.NhsNumber.ToString(), filename, screeningService.ScreeningName, "");
                return;
            }

            if (!ValidationHelper.ValidateNHSNumber(participant.NhsNumber))
            {
                // TODO: Nil return files need to be handled properly in receive
                var category = ExceptionCategory.CaaS;
                if (participant.NhsNumber == "0" || participant.NhsNumber == "0000000000")
                {
                    category = ExceptionCategory.NilReturnFile;
                }
                await _exceptionHandler.CreateSystemExceptionLog("Invalid NHS Number in CaaS file", participant, category);
                return; // skip current participant
            }

            if (!_validateDates.ValidateAllDates(participant))
            {
                await _exceptionHandler.CreateSystemExceptionLog($"Invalid effective date found in participant data", participant, ExceptionCategory.CaaS);
                return; // Skip current participant
            }

            if (!_recordsProcessTracker.RecordAlreadyProcessed(participant.RecordType, participant.NhsNumber))
            {
                await _exceptionHandler.CreateSystemExceptionLog($"Duplicate Participant was in the file", participant, ExceptionCategory.File);
                return; // Skip current participant
            }

            await AddRecordToBatch(participant, currentBatch);
        });

        if (await _callDurableDemographicFunc.PostDemographicDataAsync(currentBatch.DemographicData.ToList(), DemographicURI, filename))
        {
            await AddBatchToQueue(currentBatch);
        }
    }

    /// <summary>
    /// adds a given record to the current given batch
    /// </summary>
    /// <param name="participant"></param>
    /// <param name="currentBatch"></param>
    /// <param name="FileName"></param>
    /// <returns></returns>
    private async Task AddRecordToBatch(Participant participant, Batch currentBatch)
    {
        // take note: we don't need to add DemographicData to the queue for update because we loop through all updates in the UpdateParticipant method
        switch (participant.RecordType?.Trim())
        {

            case Actions.New:
                var DemographicRecordUpdated = await UpdateOldDemographicRecord(participant);
                currentBatch.AddRecords.Enqueue(participant);
                if (DemographicRecordUpdated)
                {
                    break;
                }

                currentBatch.DemographicData.Enqueue(participant.ToParticipantDemographic());
                break;
            case Actions.Amended:
                if (!await UpdateOldDemographicRecord(participant))
                {
                    await CreateError(participant);
                    break;
                }
                currentBatch.UpdateRecords.Enqueue(participant);
                break;
            case Actions.Removed:
                currentBatch.DeleteRecords.Enqueue(participant);
                break;
            default:
                await _exceptionHandler.CreateSystemExceptionLog("RecordType was not set to an expected value", participant, ExceptionCategory.Schema);
                break;
        }

    }

    private async Task AddBatchToQueue(Batch currentBatch)
    {
        _logger.LogInformation("sending {Count} records to queue", currentBatch.AddRecords.Count + currentBatch.UpdateRecords.Count);

        await _addBatchToQueue.ProcessBatch(currentBatch.AddRecords, _config.ParticipantManagementTopic);
        await _addBatchToQueue.ProcessBatch(currentBatch.UpdateRecords, _config.ParticipantManagementTopic);

        foreach (var updateRecords in currentBatch.DeleteRecords)
        {
            await RemoveParticipant(updateRecords);
        }
        // this used to release memory from being used
        currentBatch = null;
    }

    private async Task<bool> UpdateOldDemographicRecord(Participant participant)
    {
        try
        {
            long nhsNumber;
            if (!long.TryParse(participant.NhsNumber, out nhsNumber))
            {
                throw new FormatException("Unable to parse NHS Number");
            }

            var participantDemographic = await _participantDemographic.GetSingleByFilter(x => x.NhsNumber == nhsNumber);

            if (participantDemographic == null)
            {
                _logger.LogWarning("The participant could not be found, when trying to update old Participant");
                return false;
            }

            var participantForUpdate = participant.ToParticipantDemographic();
            participantForUpdate.ParticipantId = participantDemographic.ParticipantId;

            var updated = await _participantDemographic.Update(participantForUpdate);
            if (updated)
            {
                _logger.LogInformation("updating old Demographic record was successful");
                return updated;
            }

            _logger.LogError("updating old Demographic record was not successful");
            return updated;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update participant function failed.\nMessage: {Message}\nStack Trace: {StackTrace}", ex.Message, ex.StackTrace);
            await CreateError(participant);
        }
        return false;
    }

    // TODO: refactor now that it all uses one queue
    private async Task RemoveParticipant(Participant participant)
    {
        var allowDeleteRecords = _config.AllowDeleteRecords;
        try
        {
            if (allowDeleteRecords)
            {
                _logger.LogInformation("AllowDeleteRecords flag is true, delete record sent to RemoveParticipant function.");
                await _addBatchToQueue.AddMessage(participant, _config.ParticipantManagementTopic);
            }
            else
            {
                await _exceptionHandler.CreateSystemExceptionLog("Record received was flagged for deletion", participant, ExceptionCategory.DeleteRecord);
                _logger.LogInformation("AllowDeleteRecords flag is false, exception raised for delete record.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Remove participant function failed.\nMessage: {Message}\nStack Trace: {StackTrace}", ex.Message, ex.StackTrace);
            await CreateError(participant);
        }
    }

    private async Task CreateError(Participant participant)
    {
        try
        {
            var errorDescription = $"A record has failed to process in file {participant.Source}";
            _logger.LogError(errorDescription);
            await _exceptionHandler.CreateSystemExceptionLog(errorDescription, participant, ExceptionCategory.File);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Handling the exception failed.\nMessage: {Message}\nStack Trace: {StackTrace}", ex.Message, ex.StackTrace);
            await _exceptionHandler.CreateSystemExceptionLog(ex, participant);
        }
    }
}
