namespace LanguageCodesDataService;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Model;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using Common;
using DataServices.Core;

public class ParticipantDemographicDataService
{
    private readonly ILogger<ParticipantDemographicDataService> _logger;
    private readonly IRequestHandler<ParticipantDemographic> _requestHandler;
    private readonly ICreateResponse _createResponse;

    public ParticipantDemographicDataService(ILogger<ParticipantDemographicDataService> logger, IRequestHandler<ParticipantDemographic> requestHandler, ICreateResponse createResponse)
    {
        _logger = logger;
        _requestHandler = requestHandler;
        _createResponse = createResponse;
    }

    [Function("ParticipantDemographicDataService")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", "put", "delete", Route = "ParticipantDemographicDataService/{*key}")] HttpRequestData req, string? key)
    {
        try
        {
            _logger.LogInformation("DataService Request Received Method: {Method}, DataObject {DataType} " ,req.Method,typeof(LanguageCode));
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

