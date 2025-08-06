namespace NHS.CohortManager.ParticipantManagementService;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

using DataServices.Client;
using Common;
using System.Net;
using Model;
using System.Text.Json;

public class UpdateBlockedFlag
{
    private readonly ILogger<UpdateBlockedFlag> _logger;
    private readonly ICreateResponse _createResponse;
    private readonly IBlockParticipantHandler _blockParticipantHandler;


    public UpdateBlockedFlag(ILogger<UpdateBlockedFlag> logger, ICreateResponse createResponse, IBlockParticipantHandler blockParticipantHandler)
    {
        _logger = logger;
        _createResponse = createResponse;
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
        try
        {
            var blockParticipantDTO = await req.ReadFromJsonAsync<BlockParticipantDTO>();

            var blockParticipantResult = await _blockParticipantHandler.BlockParticipant(blockParticipantDTO);

            if (blockParticipantResult.Success)
            {
                return await _createResponse.CreateHttpResponseWithBodyAsync(HttpStatusCode.OK, req, blockParticipantResult.ResponseMessage);
            }
            return await _createResponse.CreateHttpResponseWithBodyAsync(HttpStatusCode.BadRequest, req, blockParticipantResult.ResponseMessage);
        }
        catch (JsonException jex)
        {
            _logger.LogError(jex, "Participant Block Dto couldn't be deserialized");
            return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req);
        }
        catch (FormatException fex)
        {
            _logger.LogError(fex, "Participant Block Dto couldn't be deserialized");
            return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while blocking a participant");
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
        }

    }
    [Function("GetParticipant")]
    public async Task<HttpResponseData> GetParticipantDetails([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
    {
        _logger.LogInformation("Get Participant Details Called");
        try
        {
            var blockParticipantDTO = await req.ReadFromJsonAsync<BlockParticipantDTO>();
            var getParticipantResult = await _blockParticipantHandler.GetParticipant(blockParticipantDTO);

            return await _createResponse.CreateHttpResponseWithBodyAsync(HttpStatusCode.OK, req, getParticipantResult.ResponseMessage);

        }
        catch (JsonException jex)
        {
            _logger.LogError(jex, "Participant Block Dto couldn't be deserialized");
            return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req);
        }
        catch (FormatException fex)
        {
            _logger.LogError(fex, "Participant Block Dto couldn't be deserialized");
            return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while blocking a participant");
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
        }
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
        var nhsNumber = req.Query["nhsNumber"];
        if (string.IsNullOrWhiteSpace(nhsNumber))
        {
            return await _createResponse.CreateHttpResponseWithBodyAsync(HttpStatusCode.BadRequest, req, "No NHS Number provided");
        }

        if (!ValidationHelper.ValidateNHSNumber(nhsNumber))
        {
            return await _createResponse.CreateHttpResponseWithBodyAsync(HttpStatusCode.BadRequest, req, "Invalid NHS Number provided");
        }

        var nhsNumberParsed = long.Parse(nhsNumber);

        var unBlockParticipantResult = await _blockParticipantHandler.UnblockParticipant(nhsNumberParsed);

        if (!unBlockParticipantResult.Success)
        {
            return await _createResponse.CreateHttpResponseWithBodyAsync(HttpStatusCode.BadRequest, req, unBlockParticipantResult.ResponseMessage);
        }

        return await _createResponse.CreateHttpResponseWithBodyAsync(HttpStatusCode.OK, req, "Participant successfully unblocked");

    }


}
