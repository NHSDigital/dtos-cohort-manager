namespace NHS.CohortManager.ParticipantManagementService;

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
        private readonly IDataServiceClient<ParticipantDemographic> _participantDemographicClient;
        private readonly ILogger<BlockParticipant> _logger;
        private readonly ICreateResponse _createResponse;
        private readonly IExceptionHandler _exceptionHandler;


        public BlockParticipant(IDataServiceClient<ParticipantManagement> participantManagementClient, IDataServiceClient<ParticipantDemographic> participantDemographicClient, ILogger<BlockParticipant> logger, ICreateResponse createResponse, IExceptionHandler exceptionHandler)
        {
            _participantManagementClient = participantManagementClient;
            _participantDemographicClient = participantDemographicClient;
            _logger = logger;
            _createResponse = createResponse;
            _exceptionHandler = exceptionHandler;
        }

        [Function("BlockParticipant")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
        {
            _logger.LogInformation("Block Participant Called");
            long nhsNumber;
            string dateOfBirth, familyName;
            short screeningId;
            
            try
            {
                nhsNumber = long.Parse(req.Query["NhsNumber"]);
                if (!ValidationHelper.ValidateNHSNumber(nhsNumber.ToString())) {throw new Exception("Invalid NHS Number");}

                dateOfBirth = req.Query["DateOfBirth"];
                familyName = req.Query["FamilyName"];

                screeningId = 1; //TODO Unhardcode this from BSS. (Phase 2)

                // Check participant exists in Participant Demographic table.
                ParticipantDemographic participantDemographic = await _participantDemographicClient.GetSingleByFilter(i => i.NhsNumber == nhsNumber && i.DateOfBirth == dateOfBirth && i.FamilyName == familyName);
                if (participantDemographic == null) {
                    return _createResponse.CreateHttpResponse(HttpStatusCode.NotFound, req);
                }

                ParticipantManagement participantManagement = await _participantManagementClient.GetSingleByFilter(i => i.NHSNumber == nhsNumber && i.ScreeningId == screeningId);
                participantManagement.BlockedFlag = 1;
                bool blockFlagUpdated = await _participantManagementClient.Update(participantManagement);
                
                if (!blockFlagUpdated) {throw new Exception("Failed to block participant");};
                return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req, "OK");
            }
            catch (Exception ex) 
            {
                nhsNumber = long.Parse(req.Query["NhsNumber"]);
                screeningId = 1; //TODO Unhardcode this (Phase 2)

                await _exceptionHandler.CreateSystemExceptionLogFromNhsNumber(ex, nhsNumber.ToString(), "", screeningId.ToString(), req.ToString());
                return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req, "An unknown error has occured");
            }
            
        }
}

