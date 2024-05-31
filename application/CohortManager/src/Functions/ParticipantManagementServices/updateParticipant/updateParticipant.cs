namespace updateParticipant;

using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Text;
using Model;
using System.Text.Json;
using Common;

public class UpdateParticipantFunction
{
    private readonly ILogger<UpdateParticipantFunction> _logger;
    private readonly ICreateResponse _createResponse;
    private readonly ICallFunction _callFunction;

    public UpdateParticipantFunction(ILogger<UpdateParticipantFunction> logger, ICreateResponse createResponse, ICallFunction callFunction)
    {
        _logger = logger;
        _createResponse = createResponse;
        _callFunction = callFunction;
    }

    [Function("updateParticipant")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
    {
        _logger.LogInformation("C# Update called.");
        HttpWebResponse createResponse;

        string requestBodyJson;
        using (StreamReader reader = new StreamReader(req.Body, Encoding.UTF8))
        {
            requestBodyJson = reader.ReadToEnd();
        }
        var participant = JsonSerializer.Deserialize<Participant>(requestBodyJson);

        // Any validation or decisions go in here
        if (await ValidateData(participant))
        {
            return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req);
        }

        try
        {
            var json = JsonSerializer.Serialize(participant);
            createResponse = await _callFunction.SendPost(Environment.GetEnvironmentVariable("UpdateParticipant"), json);

            if (createResponse.StatusCode == HttpStatusCode.OK)
            {
                _logger.LogInformation("participant updated");
                return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req);
            }
        }
        catch (Exception ex)
        {
            _logger.LogInformation($"Unable to call function.\nMessage:{ex.Message}\nStack Trace: {ex.StackTrace}");
        }

        _logger.LogInformation("the user has not been updated due to a bad request");

        return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req);
    }

    private async Task<bool> ValidateData(Participant participant)
    {
        var json = JsonSerializer.Serialize(participant);

        var response = await _callFunction.SendPost(Environment.GetEnvironmentVariable("StaticValidationURL"), json);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            return true;
        }

        return false;
    }
}
