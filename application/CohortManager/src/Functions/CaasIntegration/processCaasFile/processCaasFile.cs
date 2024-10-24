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

    private readonly IAzureQueueStorageHelper _azureQueueStorageHelper;

    public ProcessCaasFileFunction(ILogger<ProcessCaasFileFunction> logger, ICallFunction callFunction, ICreateResponse createResponse,
                                   ICheckDemographic checkDemographic, ICreateBasicParticipantData createBasicParticipantData, IExceptionHandler handleException,
                                   IAzureQueueStorageHelper azureQueueStorageHelper)
    {
        _logger = logger;
        _callFunction = callFunction;
        _createResponse = createResponse;
        _checkDemographic = checkDemographic;
        _createBasicParticipantData = createBasicParticipantData;
        _handleException = handleException;
        _azureQueueStorageHelper = azureQueueStorageHelper;
    }

    [Function("processCaasFile")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
    {
        string postData = "";
        using (StreamReader reader = new StreamReader(req.Body, Encoding.UTF8))
        {
            postData = reader.ReadToEnd();
        }
        var input = JsonSerializer.Deserialize<Cohort>(postData);

        _logger.LogInformation("Records received: {RecordsReceived}", input?.Participants.Count ?? 0);
        int add = 0, upd = 0, del = 0, err = 0, row = 0;

        foreach (var participant in input.Participants)
        {
            // Check the NHS number is a number
            if (!ValidationHelper.ValidateNHSNumber(participant.NhsNumber))
            {
                await _handleException.CreateSystemExceptionLog(new Exception($"Invalid NHS Number was passed at data row {row}"), participant, input.FileName);
                err++;
                continue;
            }


            // Convert string properties to DateTime? for validation
            DateTime? primaryCareDate = TryParseDate(participant.PrimaryCareProviderEffectiveFromDate);
            DateTime? addressDate = TryParseDate(participant.UsualAddressEffectiveFromDate);
            DateTime? reasonForRemovalDate = TryParseDate(participant.ReasonForRemovalEffectiveFromDate);
            DateTime? homeTelephoneDate = TryParseDate(participant.TelephoneNumberEffectiveFromDate);
            DateTime? mobileTelephoneDate = TryParseDate(participant.MobileNumberEffectiveFromDate);
            DateTime? emailAddressDate = TryParseDate(participant.EmailAddressEffectiveFromDate);
            DateTime? dateOfBirth = TryParseDate(participant.DateOfBirth);

            // Validate the date fields
            if (!IsValidDate(primaryCareDate) ||
                !IsValidDate(addressDate) ||
                !IsValidDate(reasonForRemovalDate) ||
                !IsValidDate(homeTelephoneDate) ||
                !IsValidDate(mobileTelephoneDate) ||
                !IsValidDate(emailAddressDate) ||
                !IsValidDate(dateOfBirth))
            {
                await _handleException.CreateSystemExceptionLog(new Exception($"Invalid effective date found in participant data at row {row}."), participant, input.FileName);
                err++;
                continue; // Skip this participant
            }

            row++;
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
                        var demographicDataAdded = await PostDemographicDataAsync(participant);

                        if (demographicDataAdded)
                        {
                            _azureQueueStorageHelper.AddItemsToQueueAsync("add-participant-queue", Environment.GetEnvironmentVariable("FUNCTIONS_WORKER_RUNTIME"), new List<BasicParticipantCsvRecord>() { basicParticipantCsvRecord });
                            //await _callFunction.SendPost(Environment.GetEnvironmentVariable("PMSAddParticipant"), json);
                            _logger.LogInformation("Called add participant");
                        }

                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("Add participant function failed.\nMessage: {Message}\nStack Trace: {StackTrace}", ex.Message, ex.StackTrace);
                        _handleException.CreateSystemExceptionLog(ex, participant, input.FileName);
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
                        _handleException.CreateSystemExceptionLog(ex, participant, input.FileName);
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
                        _handleException.CreateSystemExceptionLog(ex, participant, input.FileName);
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
                        _handleException.CreateSystemExceptionLog(ex, participant, input.FileName);
                    }
                    break;
            }
        }

        _logger.LogInformation("There are {add} Additions. There are {upd} Updates. There are {del} Deletions. There are {err} Errors.", add, upd, del, err);

        if (err > 0)
        {
            return _createResponse.CreateHttpResponse(HttpStatusCode.Created, req);
        }

        return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req);
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

    private static DateTime? TryParseDate(string? dateString)
    {
        if (DateTime.TryParse(dateString, out var date))
        {
            return date;
        }
        return null;
    }

    public bool IsValidDate(DateTime? date)
    {
        if (date.HasValue && date.Value > DateTime.UtcNow)
        {
            return false;
        }
        return true;
    }
}
