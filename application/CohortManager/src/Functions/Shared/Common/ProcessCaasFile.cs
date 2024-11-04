namespace Common;

using System.Text.Json;
using Common;
using Common.Interfaces;
using Microsoft.Extensions.Logging;
using Model;

public class ProcessCaasFile : IProcessCaasFile
{

    private readonly ILogger<ProcessCaasFile> _logger;
    private readonly ICallFunction _callFunction;
    private readonly ICreateResponse _createResponse;
    private readonly ICheckDemographic _checkDemographic;
    private readonly ICreateBasicParticipantData _createBasicParticipantData;
    private readonly IExceptionHandler _handleException;
    private readonly IAzureQueueStorageHelper _azureQueueStorageHelper;

    public ProcessCaasFile(ILogger<ProcessCaasFile> logger, ICallFunction callFunction, ICreateResponse createResponse, ICheckDemographic checkDemographic, ICreateBasicParticipantData createBasicParticipantData, IExceptionHandler handleException, IAzureQueueStorageHelper azureQueueStorageHelper)
    {
        _logger = logger;
        _callFunction = callFunction;
        _createResponse = createResponse;
        _checkDemographic = checkDemographic;
        _createBasicParticipantData = createBasicParticipantData;
        _handleException = handleException;
        _azureQueueStorageHelper = azureQueueStorageHelper;
    }


    public async Task ProcessRecordAsync(Participant participant, string filename)
    {
        int row = 0, add = 0, upd = 0, del = 0, err = 0;

        row++;
        var basicParticipantCsvRecord = new BasicParticipantCsvRecord
        {
            Participant = _createBasicParticipantData.BasicParticipantData(participant),
            FileName = filename,
        };
        switch (participant.RecordType?.Trim())
        {
            case Actions.New:
                add++;
                try
                {
                    var json = JsonSerializer.Serialize(basicParticipantCsvRecord);
                    var demographicDataAdded = await PostDemographicDataAsync(participant);

                    if (demographicDataAdded)
                    {
                        await _azureQueueStorageHelper.AddItemToQueueAsync<BasicParticipantCsvRecord>(basicParticipantCsvRecord, "add-participant-queue");
                        _logger.LogInformation("Called add participant");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError("Add participant function failed.\nMessage: {Message}\nStack Trace: {StackTrace}", ex.Message, ex.StackTrace);
                    _handleException.CreateSystemExceptionLog(ex, participant, filename);
                }
                break;
            case Actions.Amended:
                upd++;
                try
                {
                    var json = JsonSerializer.Serialize(basicParticipantCsvRecord);
                    var demographicDataAdded = await PostDemographicDataAsync(participant);

                    if (demographicDataAdded)
                    {
                        await _callFunction.SendPost(Environment.GetEnvironmentVariable("PMSUpdateParticipant"), json);
                        _logger.LogInformation("Called update participant");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError("Update participant function failed.\nMessage: {Message}\nStack Trace: {StackTrace}", ex.Message, ex.StackTrace);
                    _handleException.CreateSystemExceptionLog(ex, participant, filename);
                }
                break;
            case Actions.Removed:
                del++;
                try
                {
                    var json = JsonSerializer.Serialize(basicParticipantCsvRecord);
                    await _callFunction.SendPost(Environment.GetEnvironmentVariable("PMSRemoveParticipant"), json);
                    _logger.LogInformation("Called remove participant");
                }
                catch (Exception ex)
                {
                    _logger.LogError("Remove participant function failed.\nMessage: {Message}\nStack Trace: {StackTrace}", ex.Message, ex.StackTrace);
                    _handleException.CreateSystemExceptionLog(ex, participant, filename);
                }
                break;
            default:
                err++;
                try
                {

                    _logger.LogError("Cannot parse record type with action: {ParticipantRecordType}", participant.RecordType);

                    var errorDescription = $"a record has failed to process with the NHS Number : {participant.NhsNumber} because the of an incorrect record type";
                    await _handleException.CreateRecordValidationExceptionLog(participant.NhsNumber, basicParticipantCsvRecord.FileName, errorDescription, "", JsonSerializer.Serialize(participant));
                }
                catch (Exception ex)
                {
                    _logger.LogError("Handling the exception failed.\nMessage: {Message}\nStack Trace: {StackTrace}", ex.Message, ex.StackTrace);
                    _handleException.CreateSystemExceptionLog(ex, participant, filename);
                }
                break;
        }
        _logger.LogInformation("There are {add} Additions. There are {upd} Updates. There are {del} Deletions. There are {err} Errors.", add, upd, del, err);

    }

    private async Task<bool> PostDemographicDataAsync(Participant participant)
    {
        var demographicDataInserted = await _checkDemographic.PostDemographicDataAsync(participant, Environment.GetEnvironmentVariable("DemographicURI"));
        if (!demographicDataInserted)
        {
            _logger.LogError("Demographic function failed");
            return false;
        }
        return true;
    }

}