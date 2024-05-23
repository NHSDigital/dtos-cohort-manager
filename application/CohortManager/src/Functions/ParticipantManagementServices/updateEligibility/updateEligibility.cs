namespace NHS.CohortManager.CaasIntegration.UpdateEligibility;

using System.Net;
using System.Text;
using System.Text.Json;
using Common;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Model;


public class UpdateEligibility
{
    private readonly ILogger _logger;

    private readonly ICreateResponse _createResponse;

    private readonly ICallFunction _callFunction;

    public UpdateEligibility(ILogger<UpdateEligibility> logger, ICreateResponse createResponse, ICallFunction callFunction)
    {
        _logger = logger;
        _createResponse = createResponse;
        _callFunction = callFunction;
    }

    [Function("UpdateEligibility")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
    {
        _logger.LogInformation("C# Update called.");
        HttpWebResponse createResponse;

        // convert body to json and then deserialize to object
        string postdata = "";
        using (StreamReader reader = new StreamReader(req.Body, Encoding.UTF8))
        {
            postdata = reader.ReadToEnd();
        }
        var input = JsonSerializer.Deserialize<Participant>(postdata);

        // Any validation or decisions go in here

        try
        {
            var json = JsonSerializer.Serialize(input);
            createResponse = await _callFunction.SendPost(Environment.GetEnvironmentVariable("markParticipantAsEligible"), json);

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
}
