namespace updateParticipantDetails;

using System.Net;
using System.Text;
using System.Text.Json;
using Common;
using Data.Database;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Model;

public class UpdateParticipantDetails
{
    private readonly ILogger<UpdateParticipantDetails> _logger;
    private readonly ICreateResponse _createResponse;
    private readonly IUpdateParticipantData _updateParticipantData;

    public UpdateParticipantDetails(ILogger<UpdateParticipantDetails> logger, ICreateResponse createResponse, IUpdateParticipantData updateParticipant)
    {
        _logger = logger;
        _createResponse = createResponse;
        _updateParticipantData = updateParticipant;
    }

    [Function("updateParticipantDetails")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
    {
        try
        {
            string requestBody = "";
            var participantCsvRecord = new ParticipantCsvRecord();
            using (StreamReader reader = new StreamReader(req.Body, Encoding.UTF8))
            {
                requestBody = await reader.ReadToEndAsync();
                participantCsvRecord = JsonSerializer.Deserialize<ParticipantCsvRecord>(requestBody);
            }

            var isAdded = await _updateParticipantData.UpdateParticipantDetails(participantCsvRecord);
            if (isAdded)
            {
                return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req);
            }

            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message, ex);
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
        }
    }
}
