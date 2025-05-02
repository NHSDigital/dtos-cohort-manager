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

        /// <summary>
        /// Takes in a http request that if the input is valid then it updates the blocked flag within participant management table to 1. Otherwise, adds an exception to the exception table.
        /// </summary>
        /// <param name="req">
        /// A Http request that should contain an NHS number, family name, date of birth and a screening ID.
        /// </param>
        /// <returns>
        /// A http status code. (200 - Ok / 404 - Not Found / 500 - Internal server error)
        /// </returns>
        [Function("BlockParticipant")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
        {
            _logger.LogInformation("Block Participant Called");
            long? nhsNumberLong;
            string? nhsNumber, dateOfBirth, familyName;
            short screeningId;

            nhsNumber = req.Query["NhsNumber"];
            nhsNumberLong = long.Parse(nhsNumber);

            screeningId = 1; //TODO Unhardcode this (Phase 2)
            
            try
            {
                if (!ValidationHelper.ValidateNHSNumber(nhsNumber)) {throw new Exception("Invalid NHS Number");}

                dateOfBirth = req.Query["DateOfBirth"];
                familyName = req.Query["FamilyName"];

                // Check participant exists in Participant Demographic table.
                ParticipantDemographic participantDemographic = await _participantDemographicClient.GetSingleByFilter(i => i.NhsNumber == nhsNumberLong && i.DateOfBirth == dateOfBirth && i.FamilyName == familyName);
                if (participantDemographic == null) {throw new NullReferenceException("Participant can't be found");}

                ParticipantManagement participantManagement = await _participantManagementClient.GetSingleByFilter(i => i.NHSNumber == nhsNumberLong && i.ScreeningId == screeningId);
                participantManagement.BlockedFlag = 1;
                bool blockFlagUpdated = await _participantManagementClient.Update(participantManagement);
                
                if (!blockFlagUpdated) {throw new Exception("Failed to block participant");};
                return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req, "OK");
            }
            catch (NullReferenceException ex) {
                _logger.LogError("Could not find participant");
                await _exceptionHandler.CreateSystemExceptionLogFromNhsNumber(ex, nhsNumber, "", screeningId.ToString(), req.ToString());
                return _createResponse.CreateHttpResponse(HttpStatusCode.NotFound, req);
            }
            catch (Exception ex) 
            {
                _logger.LogError("Failed to block participant");
                await _exceptionHandler.CreateSystemExceptionLogFromNhsNumber(ex, nhsNumber, "", screeningId.ToString(), req.ToString());
                return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req, "An unknown error has occured");
            }
            
        }
}

