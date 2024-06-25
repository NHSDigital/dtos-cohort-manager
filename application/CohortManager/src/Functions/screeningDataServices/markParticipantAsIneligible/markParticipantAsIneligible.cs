using System.Net;
using System.Text;
using System.Text.Json;
using Common;
using Data.Database;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Model;

namespace markParticipantAsIneligible
{
    public class MarkParticipantAsIneligible
    {
        private readonly ILogger<MarkParticipantAsIneligible> _logger;
        private readonly IUpdateParticipantData _updateParticipantData;
        private readonly ICreateResponse _createResponse;

        public MarkParticipantAsIneligible(ILogger<MarkParticipantAsIneligible> logger, ICreateResponse createResponse, IUpdateParticipantData updateParticipantData)
        {
            _logger = logger;
            _updateParticipantData = updateParticipantData;
            _createResponse = createResponse;
        }

        [Function("markParticipantAsIneligible")]
        public HttpResponseData Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
        {
            string postData = "";
            using (StreamReader reader = new StreamReader(req.Body, Encoding.UTF8))
            {
                postData = reader.ReadToEnd();
            }

            var participantCsvRecord = JsonSerializer.Deserialize<ParticipantCsvRecord>(postData);
            var participant = participantCsvRecord.Participant;

            try
            {
                var updated = false;

                if (participant != null)
                {
                    updated = _updateParticipantData.UpdateParticipantAsEligible(participant, 'N');
                }
                if (updated)
                {
                    _logger.LogInformation($"record updated for participant {participant.NHSId}");
                    return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req);
                }

                _logger.LogError($"an error occurred while updating data for {participant.NHSId}");

                return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req);
            }
            catch (Exception ex)
            {
                _logger.LogError($"an error occurred: {ex}");
                return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req);
            }
        }
    }
}
