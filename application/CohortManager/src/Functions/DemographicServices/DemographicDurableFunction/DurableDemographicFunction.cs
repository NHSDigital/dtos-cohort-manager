namespace NHS.CohortManager.DemographicServices;

using System.Text;
using System.Text.Json;

using DataServices.Client;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using Model;

public class DurableDemographicFunction
{
    private readonly IDataServiceClient<ParticipantDemographic> _participantDemographic;

    private readonly ILogger<DurableDemographicFunction> _logger;

    public DurableDemographicFunction(IDataServiceClient<ParticipantDemographic> dataServiceClient, ILogger<DurableDemographicFunction> logger)
    {
        _participantDemographic = dataServiceClient;
        _logger = logger;
    }

    [Function(nameof(DurableDemographicFunction))]
    public async Task<bool> RunOrchestrator(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        _logger.LogInformation("Calling Run Orchestrator");

        var demographicJsonData = context.GetInput<string>();

        //Max number of attempts: The maximum number of attempts. If set to 1, there will be no retry.
        var retryOptions = TaskOptions.FromRetryPolicy(new RetryPolicy(
            maxNumberOfAttempts: 1,
            firstRetryInterval: TimeSpan.FromSeconds(100))
        );

        var res = await context.CallActivityAsync<bool>(nameof(InsertDemographicData), demographicJsonData, options: retryOptions);
        return res;
    }

    [Function(nameof(InsertDemographicData))]
    public async Task<bool> InsertDemographicData([ActivityTrigger] string DemographicJsonData, FunctionContext executionContext)
    {
        try
        {
            var participantData = JsonSerializer.Deserialize<List<ParticipantDemographic>>(DemographicJsonData);
            var res = await _participantDemographic.AddRange(participantData);
            return res;

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "inserting demographic data failed");
            return false;
        }
    }

    [Function("DurableDemographicFunction_HttpStart")]
    public async Task<HttpResponseData> HttpStart(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req,
        [DurableClient] DurableTaskClient client,
        FunctionContext executionContext)
    {
        // Function input comes from the request content.   
        var requestBody = "";
        using (StreamReader reader = new StreamReader(req.Body, Encoding.UTF8))
        {
            requestBody = await reader.ReadToEndAsync();
        }

        var instanceId = await client.ScheduleNewOrchestrationInstanceAsync(
            nameof(DurableDemographicFunction), requestBody);

        _logger.LogInformation("Started orchestration with ID = '{instanceId}'.", instanceId);

        // Returns an HTTP 202 response the response status 
        return await client.CreateCheckStatusResponseAsync(req, instanceId);
    }
}
