namespace NHS.CohortManager.ScreeningDataServices;

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
    private readonly IUpdateParticipantData _updateParticipantData;
    private readonly ICreateResponse _createResponse;
    private readonly IExceptionHandler _handleException;

    private readonly ICallFunction _callFunction;

    public MarkParticipantAsIneligible(ILogger<MarkParticipantAsIneligible> logger, ICreateResponse createResponse, IUpdateParticipantData updateParticipantData, ICallFunction callFunction, IExceptionHandler handleException)
    {
        _logger = logger;
        _updateParticipantData = updateParticipantData;
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
        var existingParticipantData = _updateParticipantData.GetParticipant(participantData.NhsNumber);
        if (!await ValidateData(existingParticipantData, participantData, requestBody.FileName))
        {
            _logger.LogInformation("The participant has not been removed due to a bad request.");
            return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req);
        }

        try
        {
            var updated = false;

            if (participantData != null)
            {
                updated = _updateParticipantData.UpdateParticipantAsEligible(participantData, 'N');
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
            _logger.LogError($"an error occurred: {ex}");
            await _handleException.CreateSystemExceptionLog(ex, participantData);
            return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req);
        }
    }

    private async Task<bool> ValidateData(Participant existingParticipant, Participant newParticipant, string fileName)
    {
        var json = JsonSerializer.Serialize(new LookupValidationRequestBody(existingParticipant, newParticipant, fileName));

        try
        {
            var response = await _callFunction.SendPost(Environment.GetEnvironmentVariable("LookupValidationURL"), json);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogInformation($"Lookup validation failed.\nMessage: {ex.Message}\nParticipant: {ex.StackTrace}");
            return false;
        }

        return false;
    }
}

