namespace NHS.CohortManager.ScreeningDataServices;

using System.Net;
using System.Text;
using System.Text.Json;
using Common;
using Data.Database;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using DataServices.Client;
using Model;


public class MarkParticipantAsIneligible
{
    private readonly ILogger<MarkParticipantAsIneligible> _logger;
    private readonly IDataServiceClient<ParticipantManagement> _participantManagementClient;
    private readonly ICreateResponse _createResponse;
    private readonly IExceptionHandler _handleException;

    private readonly ICallFunction _callFunction;

    public MarkParticipantAsIneligible(ILogger<MarkParticipantAsIneligible> logger, ICreateResponse createResponse, IDataServiceClient<ParticipantManagement> participantManagementClient, ICallFunction callFunction, IExceptionHandler handleException)
    {
        _logger = logger;
        _participantManagementClient = participantManagementClient;
        _createResponse = createResponse;
        _handleException = handleException;
        _callFunction = callFunction;
    }

    [Function("markParticipantAsIneligible")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
    {
        ParticipantCsvRecord requestBody;
        var existingParticipant = new Participant();

        try
        {
            using (var reader = new StreamReader(req.Body, Encoding.UTF8))
            {
                var requestBodyJson = await reader.ReadToEndAsync();
                requestBody = JsonSerializer.Deserialize<ParticipantCsvRecord>(requestBodyJson);
            }
        }
        catch
        {
            return req.CreateResponse(HttpStatusCode.BadRequest);
        }

        var participantData = requestBody.Participant;

        long nhsNumber;
        long screeningId;

        if (!long.TryParse(participantData.NhsNumber, out nhsNumber) || !long.TryParse(participantData.ScreeningId, out screeningId) )
        {
            throw new FormatException("Could not parse NhsNumber or screeningID");
        }

        // Check if a participant with the supplied NHS Number already exists
        var existingParticipantResult = await _participantManagementClient.GetByFilter(i => i.NHSNumber == nhsNumber && i.ScreeningId == screeningId);
        if (existingParticipantResult != null && existingParticipantResult.Any())
        {
            existingParticipant = new Participant(existingParticipantResult.First());
        }
        var response = await ValidateData(existingParticipant, participantData, requestBody.FileName);
        if (response.IsFatal)
        {
            _logger.LogInformation("Validation found that there was a rule that caused a fatal error to occur meaning the cohort distribution record cannot be added to the database");
            return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req);
        }

        try
        {
            var updated = false;
            var updatedParticipantManagement =  _participantManagementClient.GetSingleByFilter(x => x.NHSNumber == nhsNumber && x.ScreeningId == screeningId).Result;
            updatedParticipantManagement.EligibilityFlag = 0;
            updated = _participantManagementClient.Update(updatedParticipantManagement).Result;

            if (updated)
            {
                _logger.LogInformation("Record updated for participant {NhsNumber}", participantData.NhsNumber);
                return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req);
            }

            _logger.LogError("An error occurred while updating data for {NhsNumber}", participantData.NhsNumber);
            return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req);
        }
        catch (Exception ex)
        {
            if (ex is NullReferenceException) {
                _logger.LogError("An error occured when trying to retrieve the participant data");
            } else {
                _logger.LogError(ex, "an error occurred: {Ex}", ex);
                await _handleException.CreateSystemExceptionLog(ex, participantData, requestBody.FileName);
            }
            return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req);
        }
    }

    private async Task<ValidationExceptionLog> ValidateData(Participant existingParticipant, Participant newParticipant, string fileName)
    {
        var json = JsonSerializer.Serialize(new LookupValidationRequestBody(existingParticipant, newParticipant, fileName, Model.Enums.RulesType.ParticipantManagement));

        try
        {
            var response = await _callFunction.SendPost(Environment.GetEnvironmentVariable("LookupValidationURL"), json);
            var responseBodyJson = await _callFunction.GetResponseText(response);
            var responseBody = JsonSerializer.Deserialize<ValidationExceptionLog>(responseBodyJson);

            return responseBody;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lookup validation failed.\nMessage: {Message}\nParticipant: {NewParticipant}", ex.Message, newParticipant);
            return null;
        }
    }
}

