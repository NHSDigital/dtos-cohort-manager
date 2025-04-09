namespace NHS.Screening.BlockParticipant;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

using DataServices.Client;
using Common;
using System.Net;
using Model;

public class BlockParticipant
{
        private readonly IDataServiceClient<ParticipantManagement> _participantManagementClient;
        private readonly ILogger<BlockParticipant> _logger;
        private readonly ICreateResponse _createResponse;


        public BlockParticipant(IDataServiceClient<ParticipantManagement> participantManagementClient, ILogger<BlockParticipant> logger, ICreateResponse createResponse)
        {
            _participantManagementClient = participantManagementClient;
            _logger = logger;
            _createResponse = createResponse;
        }

        [Function("BlockParticipant")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
        {
            _logger.LogInformation("Block Participant Called");
            long nhsNumber, screeningId;
            
            try
            {
                nhsNumber = long.Parse(req.Query["NhsNumber"]);
                screeningId = 1; //TODO Unhardcode this from BSS. (Phase 2)

                var participant = await _participantManagementClient.GetSingleByFilter(i => i.NHSNumber == nhsNumber && i.ScreeningId == screeningId);
                participant.BlockedFlag = 1;
                bool blockFlagUpdated = await _participantManagementClient.Update(participant);
                
                if (!blockFlagUpdated) {throw new Exception();};
                return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req, "OK");
            } 
            catch (Exception) 
            {
                return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req, "The participant does not exist");
            }
            
        }
}

