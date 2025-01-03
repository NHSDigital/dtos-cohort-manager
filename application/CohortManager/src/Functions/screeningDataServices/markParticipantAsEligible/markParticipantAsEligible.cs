namespace markParticipantAsEligible;

using System.Net;
using System.Text;
using System.Text.Json;
using Common;
using Data.Database;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Model;

public class MarkParticipantAsEligible
{
    private readonly ILogger<MarkParticipantAsEligible> _logger;
    private readonly ICreateResponse _createResponse;
    private readonly IParticipantManagerData _participantManagerData;
    private readonly IExceptionHandler _handleException;

    public MarkParticipantAsEligible(ILogger<MarkParticipantAsEligible> logger, ICreateResponse createResponse, IParticipantManagerData participantManagerData, IExceptionHandler handleException)
    {
        _logger = logger;
        _createResponse = createResponse;
        _participantManagerData = participantManagerData;
        _handleException = handleException;
    }

    [Function("markParticipantAsEligible")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
    {
        string postData = "";
        using (StreamReader reader = new StreamReader(req.Body, Encoding.UTF8))
        {
            postData = await reader.ReadToEndAsync();
        }

        var participant = JsonSerializer.Deserialize<Participant>(postData);

        try
        {
            var updated = false;
            if (participant != null)
            {
                updated = _participantManagerData.UpdateParticipantAsEligible(participant);

            }
            if (updated)
            {
                _logger.LogInformation("Record updated for participant {NhsNumber}", participant.NhsNumber);
                return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req);
            }

            _logger.LogError("An error occurred while updating data for {NhsNumber}", participant?.NhsNumber);

            return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred: {Ex}", ex);
            _handleException.CreateSystemExceptionLog(ex, participant, "");
            return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req);
        }
    }
}
