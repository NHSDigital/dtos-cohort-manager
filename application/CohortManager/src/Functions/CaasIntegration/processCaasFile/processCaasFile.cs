namespace NHS.CohortManager.CaasIntegrationService;

using System.Net;
using System.Text;
using System.Text.Json;
using Model;
using Common;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

public class ProcessCaasFileFunction
{
    private readonly ILogger<ProcessCaasFileFunction> _logger;
    private readonly ICallFunction _callFunction;
    private readonly ICreateResponse _createResponse;
    private readonly ICheckDemographic _checkDemographic;
    private readonly ICreateBasicParticipantData _createBasicParticipantData;
    private readonly IExceptionHandler _handleException;

    public ProcessCaasFileFunction(ILogger<ProcessCaasFileFunction> logger, ICallFunction callFunction, ICreateResponse createResponse, ICheckDemographic checkDemographic, ICreateBasicParticipantData createBasicParticipantData, IExceptionHandler handleException)
    {
        _logger = logger;
        _callFunction = callFunction;
        _createResponse = createResponse;
        _checkDemographic = checkDemographic;
        _createBasicParticipantData = createBasicParticipantData;
        _handleException = handleException;
    }

    [Function("processCaasFile")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
    {
        string postData = "";
        using (StreamReader reader = new StreamReader(req.Body, Encoding.UTF8))
        {
            postData = reader.ReadToEnd();
        }
        Cohort input = JsonSerializer.Deserialize<Cohort>(postData);

        _logger.LogInformation("Records received: {RecordsReceived}", input?.Participants.Count ?? 0);
        int add = 0, upd = 0, del = 0, err = 0, row = 0;

        foreach (var participant in input.Participants)
        {
            row++;
            var demographicDataInserted = await _checkDemographic.PostDemographicDataAsync(participant, Environment.GetEnvironmentVariable("DemographicURI"));
            if (demographicDataInserted == false)
            {
                _logger.LogError("Demographic function failed");
            }

            var basicParticipantCsvRecord = new BasicParticipantCsvRecord
            {
                Participant = _createBasicParticipantData.BasicParticipantData(participant),
                FileName = input.FileName
            };

            switch (participant.RecordType?.Trim())
            {
                case Actions.New:
                    add++;
                    try
                    {
                        var json = JsonSerializer.Serialize(basicParticipantCsvRecord);
                        await _callFunction.SendPost(Environment.GetEnvironmentVariable("PMSAddParticipant"), json);
                        _logger.LogInformation("Called add participant");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("Add participant function failed.\nMessage: {Message}\nStack Trace: {StackTrace}", ex.Message, ex.StackTrace);
                        _handleException.CreateSystemExceptionLog(ex, participant);
                    }
                    break;
                case Actions.Amended:
                    upd++;
                    try
                    {
                        var json = JsonSerializer.Serialize(basicParticipantCsvRecord);
                        await _callFunction.SendPost(Environment.GetEnvironmentVariable("PMSUpdateParticipant"), json);
                        _logger.LogInformation("Called update participant");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("Update participant function failed.\nMessage: {Message}\nStack Trace: {StackTrace}", ex.Message, ex.StackTrace);
                        _handleException.CreateSystemExceptionLog(ex, participant);
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
                        _handleException.CreateSystemExceptionLog(ex, participant);
                    }
                    break;
                default:
                    err++;
                    try
                    {
                        var participantCsvRecord = new ParticipantCsvRecord
                        {
                            FileName = input.FileName,
                            Participant = participant
                        };
                        var json = JsonSerializer.Serialize(participantCsvRecord);
                        await _callFunction.SendPost(Environment.GetEnvironmentVariable("StaticValidationURL"), json);
                        _logger.LogInformation("Called static validation");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("Static validation function failed.\nMessage: {Message}\nStack Trace: {StackTrace}", ex.Message, ex.StackTrace);
                        _handleException.CreateSystemExceptionLog(ex, participant);
                    }
                    break;
            }
        }

        _logger.LogInformation("There are {add} Additions. There are {upd} Updates. There are {del} Deletions. There are {err} Errors.", add, upd, del, err);

        if (err > 0)
        {
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
        }

        return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req);
    }
}
