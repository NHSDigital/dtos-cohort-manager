using System.Net;
using System.Text;
using System.Text.Json;
using Common;
using Data.Database;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Model;

namespace markParticipantAsEligible
{
    public class MarkParticipantAsEligible
    {
        private readonly ILogger<MarkParticipantAsEligible> _logger;
        private readonly ICreateResponse _createResponse;
        private readonly IUpdateParticipantData _updateParticipantData;
        private readonly IExceptionHandler _handleException;

        public MarkParticipantAsEligible(ILogger<MarkParticipantAsEligible> logger, ICreateResponse createResponse, IUpdateParticipantData updateParticipant, IExceptionHandler handleException)
        {
            _logger = logger;
            _createResponse = createResponse;
            _updateParticipantData = updateParticipant;
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
                    updated = _updateParticipantData.UpdateParticipantAsEligible(participant, 'Y');

                }
                if (updated)
                {
                    _logger.LogInformation($"record updated for participant {participant.NhsNumber}");
                    return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req);
                }

                _logger.LogError($"an error occurred while updating data for {participant.NhsNumber}");

                return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req);
            }
            catch (Exception ex)
            {
                _logger.LogError($"an error occurred: {ex}");
                _handleException.CreateSystemExceptionLog(ex, participant);
                return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req);
            }
        }
    }
}
