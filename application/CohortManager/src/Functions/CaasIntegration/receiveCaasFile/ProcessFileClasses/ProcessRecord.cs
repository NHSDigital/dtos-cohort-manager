namespace NHS.Screening.ReceiveCaasFile;

using System.Text.Json;
using Common;
using Data.Database;
using Microsoft.Extensions.Logging;
using Model;

public class ProcessRecord : IProcessRecord
{
    private readonly ILogger<ProcessRecord> _logger;
    private readonly IExceptionHandler _exceptionHandler;
    private readonly ICheckDemographic _checkDemographic;
    private readonly ICallFunction _callFunction;

    public ProcessRecord(ILogger<ProcessRecord> logger, IExceptionHandler exceptionHandler, ICheckDemographic checkDemographic, ICallFunction callFunction)
    {
        _logger = logger;
        _exceptionHandler = exceptionHandler;
        _checkDemographic = checkDemographic;
        _callFunction = callFunction;
    }

    public async Task UpdateParticipant(BasicParticipantCsvRecord basicParticipantCsvRecord, string filename)
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
            _logger.LogError(ex, "Update participant function failed.\nMessage: {Message}\nStack Trace: {StackTrace}", ex.Message, ex.StackTrace);
            await CreateError(basicParticipantCsvRecord.participant, filename);
        }
    }

    public async Task RemoveParticipant(BasicParticipantCsvRecord basicParticipantCsvRecord, string filename)
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
            var errorDescription = $"a record has failed to process with the NHS Number : {participant.NhsNumber} because the of an incorrect record type";
            await _exceptionHandler.CreateRecordValidationExceptionLog(participant.NhsNumber, filename, errorDescription, "", JsonSerializer.Serialize(participant));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Handling the exception failed.\nMessage: {Message}\nStack Trace: {StackTrace}", ex.Message, ex.StackTrace);
            await _exceptionHandler.CreateSystemExceptionLog(ex, participant, filename);
        }
    }
}
