namespace NHS.Screening.ReceiveCaasFile;

using System.Text.Json;
using Common;
using Common.Interfaces;
using Data.Database;
using DataServices.Client;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols.Configuration;
using Model;

public class ProcessCaasFile : IProcessCaasFile
{
    private readonly ILogger<ProcessCaasFile> _logger;
    private readonly ICallFunction _callFunction;
    private readonly IReceiveCaasFileHelper _receiveCaasFileHelper;
    private readonly ICheckDemographic _checkDemographic;
    private readonly ICreateBasicParticipantData _createBasicParticipantData;
    private readonly IAddBatchToQueue _addBatchToQueue;
    private readonly IExceptionHandler _exceptionHandler;
    private readonly IDataServiceClient<ParticipantDemographic> _participantDemographic;
    private readonly IRecordsProcessedTracker _recordsProcessTracker;
    private readonly IValidateDates _validateDates;
    private readonly string DemographicURI;
    private readonly string PMSUpdateParticipant;
    private readonly string AddParticipantQueueName;
    private readonly string UpdateParticipantQueueName;


    public ProcessCaasFile(
        ILogger<ProcessCaasFile> logger,
        ICallFunction callFunction,
        ICheckDemographic checkDemographic,
        ICreateBasicParticipantData createBasicParticipantData,
        IAddBatchToQueue addBatchToQueue,
        IReceiveCaasFileHelper receiveCaasFileHelper,
        IExceptionHandler exceptionHandler,
        IDataServiceClient<ParticipantDemographic> participantDemographic,
        IRecordsProcessedTracker recordsProcessedTracker,
        IValidateDates validateDates
    )
    {
        _logger = logger;
        _callFunction = callFunction;
        _checkDemographic = checkDemographic;
        _createBasicParticipantData = createBasicParticipantData;
        _addBatchToQueue = addBatchToQueue;
        _receiveCaasFileHelper = receiveCaasFileHelper;
        _exceptionHandler = exceptionHandler;
        _participantDemographic = participantDemographic;
        _recordsProcessTracker = recordsProcessedTracker;
        _validateDates = validateDates;

        DemographicURI = Environment.GetEnvironmentVariable("DemographicURI");
        PMSUpdateParticipant = Environment.GetEnvironmentVariable("PMSUpdateParticipant");
        AddParticipantQueueName = Environment.GetEnvironmentVariable("AddQueueName");
        UpdateParticipantQueueName = Environment.GetEnvironmentVariable("UpdateQueueName");


        if (string.IsNullOrEmpty(DemographicURI) || string.IsNullOrEmpty(PMSUpdateParticipant) || string.IsNullOrEmpty(AddParticipantQueueName) || string.IsNullOrEmpty(UpdateParticipantQueueName))
        {
            _logger.LogError("Required environment variables DemographicURI and PMSUpdateParticipant are missing.");
            throw new InvalidConfigurationException("Required environment variables DemographicURI and PMSUpdateParticipant are missing.");
        }
    }

    /// <summary>
    /// process a given batch and send it the queue
    /// </summary>
    /// <param name="values"></param>
    /// <param name="options"></param>
    /// <param name="screeningService"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    public async Task ProcessRecords(List<ParticipantsParquetMap> values, ParallelOptions options, ScreeningService screeningService, string name)
    {
        var currentBatch = new Batch();
        await Parallel.ForEachAsync(values, options, async (rec, cancellationToken) =>
        {
            var participant = await _receiveCaasFileHelper.MapParticipant(rec, screeningService.ScreeningId, screeningService.ScreeningName, name);

            if (participant == null)
            {
                return;
            }

            if (!ValidationHelper.ValidateNHSNumber(participant.NhsNumber))
            {
                await _exceptionHandler.CreateSystemExceptionLog(new Exception($"Invalid NHS Number was passed in for participant {participant} and file {name}"), participant, name);
                return; // skip current participant
            }

            if (!_validateDates.ValidateAllDates(participant))
            {
                await _exceptionHandler.CreateSystemExceptionLog(new Exception($"Invalid effective date found in participant data {participant} and file name {name}"), participant, name);
                return; // Skip current participant
            }

            if (!_recordsProcessTracker.RecordAlreadyProcessed(participant.RecordType, participant.NhsNumber))
            {
                await _exceptionHandler.CreateSystemExceptionLog(new Exception($"Duplicate Participant was in the file"), participant, name);
                return; // Skip current participant
            }

            await AddRecordToBatch(participant, currentBatch, name);
        });

        if (await _checkDemographic.PostDemographicDataAsync(currentBatch.DemographicData.ToList(), Environment.GetEnvironmentVariable("DemographicURI")))
        {
            await AddBatchToQueue(currentBatch, name);
        }
    }

    /// <summary>
    /// adds a given record to the current given batch
    /// </summary>
    /// <param name="participant"></param>
    /// <param name="currentBatch"></param>
    /// <param name="FileName"></param>
    /// <returns></returns>
    private async Task<Batch> AddRecordToBatch(Participant participant, Batch currentBatch, string fileName)
    {
        var basicParticipantCsvRecord = new BasicParticipantCsvRecord
        {
            Participant = _createBasicParticipantData.BasicParticipantData(participant),
            FileName = fileName,
            participant = participant
        };
        // take note: we don't need to add DemographicData to the queue for update because we loop through all updates in the UpdateParticipant method
        switch (participant.RecordType?.Trim())
        {
            case Actions.New:
                currentBatch.DemographicData.Enqueue(participant.ToParticipantDemographic());
                currentBatch.AddRecords.Enqueue(basicParticipantCsvRecord);
                break;
            case Actions.Amended:

                var deleted = await DeleteOldDemographicRecord(basicParticipantCsvRecord, fileName);
                if (!deleted)
                {
                    _logger.LogError("Could not delete old demographic participant with participant Id: {ParticipantId}", basicParticipantCsvRecord.participant.ParticipantId);
                    break;
                }
                currentBatch.DemographicData.Enqueue(participant.ToParticipantDemographic());
                currentBatch.UpdateRecords.Enqueue(basicParticipantCsvRecord);

                break;
            case Actions.Removed:
                currentBatch.DeleteRecords.Enqueue(basicParticipantCsvRecord);
                break;
            default:
                await _exceptionHandler.CreateSchemaValidationException(basicParticipantCsvRecord, "RecordType was not set to an expected value");
                break;
        }
        return currentBatch;

    }

    private async Task AddBatchToQueue(Batch currentBatch, string name)
    {
        _logger.LogInformation("sending {count} records to queue", currentBatch.AddRecords.Count);

        await _addBatchToQueue.ProcessBatch(currentBatch.AddRecords, AddParticipantQueueName);

        if (currentBatch.UpdateRecords.LongCount() > 0 || currentBatch.DeleteRecords.LongCount() > 0)
        {
            _logger.LogInformation("sending Update Records {count} to queue", currentBatch.UpdateRecords.Count);
            await _addBatchToQueue.ProcessBatch(currentBatch.UpdateRecords, UpdateParticipantQueueName);

            foreach (var updateRecords in currentBatch.DeleteRecords)
            {
                await RemoveParticipant(updateRecords, name);
            }
        }
        // this used to release memory from being used
        currentBatch = null;
    }

    private async Task<bool> DeleteOldDemographicRecord(BasicParticipantCsvRecord basicParticipantCsvRecord, string name)
    {
        try
        {
            var json = JsonSerializer.Serialize(basicParticipantCsvRecord);
            var listOfData = new List<ParticipantDemographic>()
            {
                basicParticipantCsvRecord.participant.ToParticipantDemographic()
            };

            long nhsNumber;

            if (!long.TryParse(basicParticipantCsvRecord.participant.NhsNumber, out nhsNumber))
            {
                throw new FormatException("Unable to parse NHS Number");
            }

            var participant = await _participantDemographic.GetSingleByFilter(x => x.NhsNumber == nhsNumber);

            if (participant != null)
            {
                var deleted = await _participantDemographic.Delete(participant.ParticipantId.ToString());

                _logger.LogInformation(deleted ? "Deleting old Demographic record was successful" : "Deleting old Demographic record was not successful");
                return deleted;
            }
            else
            {
                _logger.LogWarning("The participant could not be found, preventing updates from being applied");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update participant function failed.\nMessage: {Message}\nStack Trace: {StackTrace}", ex.Message, ex.StackTrace);
            await CreateError(basicParticipantCsvRecord.participant, name);
        }
        return false;
    }

    private async Task RemoveParticipant(BasicParticipantCsvRecord basicParticipantCsvRecord, string filename)
    {
        var allowDeleteRecords = (bool)DatabaseHelper.ConvertBoolStringToBoolByType("AllowDeleteRecords", DataTypes.Boolean);
        try
        {
            if (allowDeleteRecords)
            {
                _logger.LogInformation("AllowDeleteRecords flag is true, delete record will be sent to removeParticipant function in a future PR.");
            }
            else
            {
                await _exceptionHandler.CreateDeletedRecordException(basicParticipantCsvRecord);
                _logger.LogInformation("AllowDeleteRecords flag is false, exception raised for delete record.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Remove participant function failed.\nMessage: {Message}\nStack Trace: {StackTrace}", ex.Message, ex.StackTrace);
            await CreateError(basicParticipantCsvRecord.participant, filename);
        }
    }

    private async Task CreateError(Participant participant, string filename)
    {
        try
        {
            _logger.LogError("Cannot parse record type with action: {ParticipantRecordType}", participant.RecordType);
            var errorDescription = $"a record has failed to process with the NHS Number: REDACTED because of an incorrect record type";
            await _exceptionHandler.CreateRecordValidationExceptionLog(participant.NhsNumber, filename, errorDescription, "", JsonSerializer.Serialize(participant));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Handling the exception failed.\nMessage: {Message}\nStack Trace: {StackTrace}", ex.Message, ex.StackTrace);
            await _exceptionHandler.CreateSystemExceptionLog(ex, participant, filename);
        }
    }
}
