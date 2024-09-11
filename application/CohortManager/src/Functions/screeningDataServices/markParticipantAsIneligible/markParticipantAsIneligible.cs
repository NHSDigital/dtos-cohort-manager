namespace NHS.CohortManager.ScreeningDataServices;

using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.Json;
using Common;
using Data.Database;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Model;


public class MarkParticipantAsIneligible
{
    private readonly ILogger<MarkParticipantAsIneligible> _logger;
    private readonly IParticipantManagerData _participantManagerData;
    private readonly ICreateResponse _createResponse;
    private readonly IExceptionHandler _handleException;

    private readonly ICallFunction _callFunction;

    public MarkParticipantAsIneligible(ILogger<MarkParticipantAsIneligible> logger, ICreateResponse createResponse, IParticipantManagerData participantManagerData, ICallFunction callFunction, IExceptionHandler handleException)
    {
        _logger = logger;
        _participantManagerData = participantManagerData;
        _createResponse = createResponse;
        _handleException = handleException;
        _callFunction = callFunction;
    }

    [Function("markParticipantAsIneligible")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
    {
        ParticipantCsvRecord requestBody;

        try
        {
            using (var reader = new StreamReader(req.Body, Encoding.UTF8))
            {
                var requestBodyJson = reader.ReadToEnd();
                requestBody = JsonSerializer.Deserialize<ParticipantCsvRecord>(requestBodyJson);
            }
        }
        catch
        {
            return req.CreateResponse(HttpStatusCode.BadRequest);
        }

        var participantData = requestBody.Participant;

        // Check if a participant with the supplied NHS Number already exists
        var existingParticipantData = _participantManagerData.GetParticipant(participantData.NhsNumber, participantData.ScreeningId);
        var response = await ValidateData(existingParticipantData, participantData, requestBody.FileName);
        if (response.IsFatal)
        {
            _logger.LogInformation("Validation found that there was a rule that caused a fatal error to occur meaning the cohort distribution record cannot be added to the database");

            return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req);
        }

        try
        {
            var updated = false;

            if (participantData != null)
            {
                updated = _participantManagerData.UpdateParticipantAsEligible(participantData, 'N');
            }
            if (updated)
            {
                _logger.LogInformation($"record updated for participant {participantData.NhsNumber}");
                return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req);
            }

            _logger.LogError($"an error occurred while updating data for {participantData.NhsNumber}");

            return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,$"an error occurred: {ex}");
            await _handleException.CreateSystemExceptionLog(ex, participantData, requestBody.FileName);
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
            _logger.LogError(ex,$"Lookup validation failed.\nMessage: {ex.Message}\nParticipant: {newParticipant}");
            return null;
        }
    }
}

