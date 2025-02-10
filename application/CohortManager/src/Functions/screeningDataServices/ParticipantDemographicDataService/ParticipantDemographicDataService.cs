namespace LanguageCodesDataService;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Model;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using Common;
using DataServices.Core;
using System.Text.Json;

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
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", "put", "delete", Route = "ParticipantDemographicDataService/action/{*key}")] HttpRequestData req, string? key)
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

    [Function("RetrievePDSDemographic")]
    public async Task<HttpResponseData> RetrievePDSDemographic(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "ParticipantDemographicDataService/retrieve-pds/{*key}")] HttpRequestData req,
        string? key)
    {
        _logger.LogInformation($"Received request to check NHS Number for key: {key}");

        try
        {
            // Fetch the data based on the key
            var result = await _requestHandler.HandleRequest(req, key);

            // Convert the response to a Stream and read it as a string
            using var reader = new StreamReader(result.Body);
            string responseBody = await reader.ReadToEndAsync();

            // Deserialize the response into a dictionary
            var record = JsonSerializer.Deserialize<Dictionary<string, object>>(responseBody);

            if (record != null && record.ContainsKey("NhsNumber") && !string.IsNullOrEmpty(record["NhsNumber"]?.ToString()))
            {
                _logger.LogInformation($"NHS Number found for key {key}");
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteStringAsync($"NHS Number exists for key {key}.");
                return response;
            }
            else
            {
                _logger.LogWarning($"No NHS Number found for key {key}");
                var response = req.CreateResponse(HttpStatusCode.NotFound);
                await response.WriteStringAsync($"No NHS Number exists for key {key}.");
                return response;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error occurred while checking NHS Number for key {key}");
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            await response.WriteStringAsync($"Error: {ex.Message}");
            return response;
        }
    }

}

