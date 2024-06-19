namespace NHS.CohortManager.DemographicServices;

using System.Net;
using System.Text;
using System.Text.Json;
using Common;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Model;

public class DemographicDataFunction
{
    private readonly ILogger<DemographicDataFunction> _logger;
    private readonly ICreateResponse _createResponse;
    private readonly ICallFunction _callFunction;

    public DemographicDataFunction(ILogger<DemographicDataFunction> logger, ICreateResponse createResponse, ICallFunction callFunction)
    {
        _logger = logger;
        _createResponse = createResponse;
        _callFunction = callFunction;
    }

    [Function("DemographicDataFunction")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
    {
        var participantData = new Participant();
        try
        {
            if (req.Method == "POST")
            {
                using (StreamReader reader = new StreamReader(req.Body, Encoding.UTF8))
                {
                    var requestBody = await reader.ReadToEndAsync();
                    participantData = JsonSerializer.Deserialize<Participant>(requestBody);
                }

                var res = await _callFunction.SendPost(Environment.GetEnvironmentVariable("DemographicDataServiceURI"), JsonSerializer.Serialize(participantData));

                if (res.StatusCode != HttpStatusCode.OK)
                {
                    _logger.LogInformation("demographic function failed");
                    return _createResponse.CreateHttpResponse(res.StatusCode, req);
                }
            }
            else
            {
                var functionUrl = Environment.GetEnvironmentVariable("DemographicDataServiceURI");
                string Id = req.Query["Id"];

                var data = await _callFunction.SendGet($"{functionUrl}?Id={Id}");

                if (string.IsNullOrEmpty(data))
                {
                    _logger.LogInformation("demographic function failed");
                    return _createResponse.CreateHttpResponse(HttpStatusCode.NotFound, req);
                }
                return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req, data);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"there has been an error saving demographic data: {ex.Message}");
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
        }
        return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req);
    }
}
