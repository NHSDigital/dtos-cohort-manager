namespace ParticipantManagementDataService;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using DataServices.Database;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using Common;
using DataServices.Core;
using Model;

public class ParticipantManagementDataService
{
    private readonly ILogger<ParticipantManagementDataService> _logger;
    private readonly IRequestHandler<ParticipantManagement> _requestHandler;
    private readonly ICreateResponse _createResponse;

    public ParticipantManagementDataService(ILogger<ParticipantManagementDataService> logger, IRequestHandler<ParticipantManagement> requestHandler, ICreateResponse createResponse)
    {
        _logger = logger;
        _requestHandler = requestHandler;
        _createResponse = createResponse;
    }

    [Function("ParticipantManagementDataService")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", "put", "delete", Route = "ParticipantManagementDataService/{*key}")] HttpRequestData req, string? key)
    {
        try
        {
            _logger.LogInformation("DataService Request Received Method: {Method}, DataObject {dataType} " ,req.Method,typeof(CurrentPosting));
            var result = await _requestHandler.HandleRequest(req, key);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error has occurred ");
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req, $"An error has occurred {ex.Message}");
        }
    }

}
