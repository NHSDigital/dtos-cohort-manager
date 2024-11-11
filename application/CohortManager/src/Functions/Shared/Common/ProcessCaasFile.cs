namespace Common;

using System.Text.Json;
using Common;
using Common.Interfaces;
using Microsoft.Extensions.Logging;
using Model;
using System.Collections.Concurrent;
using Azure.Storage.Queues;

public class ProcessCaasFile : IProcessCaasFile
{

    private readonly ILogger<ProcessCaasFile> _logger;
    private readonly ICallFunction _callFunction;

    private readonly ICheckDemographic _checkDemographic;
    private readonly ICreateBasicParticipantData _createBasicParticipantData;
    private readonly IExceptionHandler _handleException;
    private readonly IAzureQueueStorageHelper _azureQueueStorageHelper;

    public ProcessCaasFile(ILogger<ProcessCaasFile> logger, ICallFunction callFunction, ICheckDemographic checkDemographic, ICreateBasicParticipantData createBasicParticipantData, IExceptionHandler handleException, IAzureQueueStorageHelper azureQueueStorageHelper)
    {
        _logger = logger;
        _callFunction = callFunction;
        _checkDemographic = checkDemographic;
        _createBasicParticipantData = createBasicParticipantData;
        _handleException = handleException;
        _azureQueueStorageHelper = azureQueueStorageHelper;
    }

    public async Task<QueueClient> CreateAddQueueCLient()
    {
        return await _azureQueueStorageHelper.CreateAddQueue();
    }

    public async Task AddBatchToQueue(Batch currentBatch, string name)
    {
        //int row = 0, add = 0, upd = 0, del = 0, err = 0;
        //row++;
        _logger.LogInformation("sending {count} records to queue", currentBatch.AddRecords.Count);
        var foo = await _azureQueueStorageHelper.ProcessBatch(currentBatch);

        if (currentBatch.UpdateRecords.LongCount() > 0 || currentBatch.DeleteRecords.LongCount() > 0)
        {
            foreach (var updateRecords in currentBatch.UpdateRecords)
            {
                await UpdateParticipant(updateRecords, name);
            }

            foreach (var updateRecords in currentBatch.DeleteRecords)
            {
                await RemoveParticipant(updateRecords, name);
            }
        }
    }

    private async Task UpdateParticipant(BasicParticipantCsvRecord basicParticipantCsvRecord, string name)
    {
        try
        {
            var json = JsonSerializer.Serialize(basicParticipantCsvRecord);
            if (await _checkDemographic.PostDemographicDataAsync(basicParticipantCsvRecord.participant, Environment.GetEnvironmentVariable("DemographicURI")))
            {
                await _callFunction.SendPost(Environment.GetEnvironmentVariable("PMSUpdateParticipant"), json);
            }
            _logger.LogInformation("Called update participant");

        }
        catch (Exception ex)
        {
            _logger.LogError("Update participant function failed.\nMessage: {Message}\nStack Trace: {StackTrace}", ex.Message, ex.StackTrace);
            _handleException.CreateSystemExceptionLog(ex, basicParticipantCsvRecord.participant, name);
            await CreateError(basicParticipantCsvRecord.participant, name);
        }
    }

    private async Task RemoveParticipant(BasicParticipantCsvRecord basicParticipantCsvRecord, string filename)
    {
        try
        {
            var json = JsonSerializer.Serialize(basicParticipantCsvRecord);
            await _callFunction.SendPost(Environment.GetEnvironmentVariable("PMSRemoveParticipant"), json);
            _logger.LogInformation("Called remove participant");
        }
        catch (Exception ex)
        {
            _logger.LogError("Remove participant function failed.\nMessage: {Message}\nStack Trace: {StackTrace}", ex.Message, ex.StackTrace);
            _handleException.CreateSystemExceptionLog(ex, basicParticipantCsvRecord.participant, filename);
            await CreateError(basicParticipantCsvRecord.participant, filename);
        }
    }

    private async Task CreateError(Participant participant, string filename)
    {
        try
        {
            _logger.LogError("Cannot parse record type with action: {ParticipantRecordType}", participant.RecordType);
            var errorDescription = $"a record has failed to process with the NHS Number : {participant.NhsNumber} because the of an incorrect record type";
            await _handleException.CreateRecordValidationExceptionLog(participant.NhsNumber, filename, errorDescription, "", JsonSerializer.Serialize(participant));
        }
        catch (Exception ex)
        {
            _logger.LogError("Handling the exception failed.\nMessage: {Message}\nStack Trace: {StackTrace}", ex.Message, ex.StackTrace);
            _handleException.CreateSystemExceptionLog(ex, participant, filename);
        }
    }

}
