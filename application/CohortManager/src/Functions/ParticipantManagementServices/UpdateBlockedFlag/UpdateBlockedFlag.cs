namespace NHS.CohortManager.ParticipantManagementService;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

using DataServices.Client;
using Common;
using System.Net;
using Model;

public class UpdateBlockedFlag
{
    private readonly IDataServiceClient<ParticipantManagement> _participantManagementClient;
    private readonly IDataServiceClient<ParticipantDemographic> _participantDemographicClient;
    private readonly ILogger<UpdateBlockedFlag> _logger;
    private readonly ICreateResponse _createResponse;
    private readonly IExceptionHandler _exceptionHandler;
    private readonly IBlockParticipantHandler _blockParticipantHandler;


    public UpdateBlockedFlag(IDataServiceClient<ParticipantManagement> participantManagementClient, IDataServiceClient<ParticipantDemographic> participantDemographicClient, ILogger<UpdateBlockedFlag> logger, ICreateResponse createResponse, IExceptionHandler exceptionHandler, IBlockParticipantHandler blockParticipantHandler)
    {
        _participantManagementClient = participantManagementClient;
        _participantDemographicClient = participantDemographicClient;
        _logger = logger;
        _createResponse = createResponse;
        _exceptionHandler = exceptionHandler;
        _blockParticipantHandler = blockParticipantHandler;
    }

    /// <summary>
    /// Takes in a http request that if the input is valid then it updates the blocked flag within participant management table to 1.
    /// Otherwise, adds an exception to the exception table.
    /// </summary>
    /// <param name="req">
    /// A Http request that should contain an NHS number, family name, date of birth and a screening ID.
    /// </param>
    /// <returns>
    /// A http status code. (200 - Ok / 404 - Not Found / 500 - Internal server error)
    /// </returns>
    [Function("BlockParticipant")]
    public async Task<HttpResponseData> BlockParticipant([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
    {
        _logger.LogInformation("Block Participant Called");

        var blockParticipantDTO = await req.ReadFromJsonAsync<BlockParticipantDTO>();

        var blockParticipantResult = await _blockParticipantHandler.BlockParticipant(blockParticipantDTO);

        if (blockParticipantResult.Success)
        {
            return await _createResponse.CreateHttpResponseWithBodyAsync(HttpStatusCode.OK, req, blockParticipantResult.ResponseMessage);
        }
        return await _createResponse.CreateHttpResponseWithBodyAsync(HttpStatusCode.BadRequest, req, blockParticipantResult.ResponseMessage);

    }

    /// <summary>
    /// Takes in a http request that if the input is valid then it updates the blocked flag within participant management table to 0.
    /// Otherwise, adds an exception to the exception table.
    /// </summary>
    /// <param name="req">
    /// A Http request that should contain an NHS number, family name, date of birth and a screening ID.
    /// </param>
    /// <returns>
    /// A http status code. (200 - Ok / 404 - Not Found / 500 - Internal server error)
    /// </returns>
    [Function("UnblockParticipant")]
    public async Task<HttpResponseData> UnblockParticipant([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
    {
        _logger.LogInformation("Unblock Participant Called");
        return await Main(0, req);
    }


    // [Function("PreviewParticipant")]
    // public async Task<HttpResponseData> PreviewParticipant([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
    // {

    // }

    private async Task<HttpResponseData> Main(short BlockedFlag, HttpRequestData req)
    {
        try
        {
            string nhsNumberStr = req.Query["NhsNumber"]!;
            string dateOfBirth = req.Query["DateOfBirth"]!;
            string familyName = req.Query["FamilyName"]!;

            if (string.IsNullOrWhiteSpace(nhsNumberStr) ||
                string.IsNullOrWhiteSpace(dateOfBirth) ||
                string.IsNullOrWhiteSpace(familyName))
            {
                throw new InvalidDataException("Missing or empty required query parameters.");
            }

            long nhsNumber = long.Parse(nhsNumberStr);
            if (!ValidationHelper.ValidateNHSNumber(nhsNumberStr))
            {
                throw new InvalidDataException("Invalid NHS Number");
            }

            // Check participant exists in Participant Demographic table.
            ParticipantDemographic participantDemographic = await _participantDemographicClient
                .GetSingleByFilter(i => i.NhsNumber == nhsNumber && i.DateOfBirth == dateOfBirth && i.FamilyName == familyName);

            ParticipantManagement participantManagement;
            bool blockFlagUpdated;
            if (participantDemographic == null && BlockedFlag == 1)
            {
                // //Search PDS
                // //participantManagement = CheckPDS(nhsNumber, dateOfBirth, familyName);   //TODO Remove Mock and add call to PDS.
                // // If PDS returns non 200
                // if (participantManagement == null) //TODO This will become conclave expression when we can call PDS like a DataService
                // {
                //     throw new KeyNotFoundException("Could not find participant");
                // }
                // else
                // {
                //     participantManagement.ScreeningId = 1;
                //     blockFlagUpdated = await _participantManagementClient.Add(participantManagement);
                // }
            }
            else if (participantDemographic == null && BlockedFlag == 0)
            {
                throw new KeyNotFoundException("Could not find participant");
            }
            else
            {
                participantManagement = await _participantManagementClient.GetSingleByFilter(i => i.NHSNumber == nhsNumber && i.ScreeningId == 1); // TODO Unhardcode this (Phase 2)
                // Change blocked flag
                participantManagement.BlockedFlag = BlockedFlag;
                blockFlagUpdated = await _participantManagementClient.Update(participantManagement);
            }

            if (!true)
                throw new HttpRequestException("Failed to update blocked flag");

            return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req, "Blocked flag updated successfully");
        }
        catch (InvalidDataException ex)
        {
            await HandleException(ex, req);
            return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req, "Invalid NHS Number or missing parameters");
        }
        catch (KeyNotFoundException ex)
        {
            await HandleException(ex, req);
            return _createResponse.CreateHttpResponse(HttpStatusCode.NotFound, req, "Participant not found");
        }
        catch (Exception ex)
        {
            await HandleException(ex, req);
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req, "An error occurred while processing the request");
        }

    }

    private async Task HandleException(Exception ex, HttpRequestData req)
    {
        _logger.LogError(ex, "An error occurred while processing the request for blocking/unblocking a participant");
        await _exceptionHandler.CreateSystemExceptionLogFromNhsNumber(ex, req.Query["NhsNumber"]!, "", "1", req.ToString()!);
    }

}
