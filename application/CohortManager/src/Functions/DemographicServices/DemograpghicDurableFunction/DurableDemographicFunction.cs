namespace NHS.CohortManager.DemographicServices;

using System.Text;
using System.Text.Json;
using Apache.Arrow;
using Data.Database;
using DurableTask.Core;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using Model;

public class DurableDemographicFunction
{

    private readonly ICreateDemographicData _createDemographicData;

    public DurableDemographicFunction(ICreateDemographicData createDemographicData)
    {
        _createDemographicData = createDemographicData;
    }

    [Function(nameof(DurableDemographicFunction))]
    public async Task<bool> RunOrchestrator(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        ILogger logger = context.CreateReplaySafeLogger(nameof(DurableDemographicFunction));
        logger.LogInformation("Calling Run Orchestrator");

        var demographicJsonData = context.GetInput<string>();

        //Max number of attempts: The maximum number of attempts. If set to 1, there will be no retry.
        var retryOptions = TaskOptions.FromRetryPolicy(new RetryPolicy(
            maxNumberOfAttempts: 1,
            firstRetryInterval: TimeSpan.FromSeconds(5))
        );

        var res = await context.CallActivityAsync<bool>(nameof(InsertDemographicData), demographicJsonData, options: retryOptions);
        return res;
    }

    [Function(nameof(InsertDemographicData))]
    public async Task<bool> InsertDemographicData([ActivityTrigger] string DemographicJsonData, FunctionContext executionContext)
    {
        var logger = executionContext.GetLogger("InsertDemographicData");
        try
        {
            var participantData = JsonSerializer.Deserialize<List<Demographic>>(DemographicJsonData);

            var res = await _createDemographicData.InsertDemographicData(participantData);
            return res;

        }
        catch (Exception ex)
        {
            logger.LogError(ex, "inserting demographic data failed");
            return false;
        }
    }

    [Function("DurableDemographicFunction_HttpStart")]
    public async Task<HttpResponseData> HttpStart(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req,
        [DurableClient] DurableTaskClient client,
        FunctionContext executionContext)
    {
        ILogger logger = executionContext.GetLogger("DurableDemographicFunction_HttpStart");

        // Function input comes from the request content.   
        var requestBody = "";
        using (StreamReader reader = new StreamReader(req.Body, Encoding.UTF8))
        {
            requestBody = await reader.ReadToEndAsync();
        }

        var instanceId = await client.ScheduleNewOrchestrationInstanceAsync(
            nameof(DurableDemographicFunction), requestBody);

        logger.LogInformation("Started orchestration with ID = '{instanceId}'.", instanceId);

        // Returns an HTTP 202 response with an instance management payload.
        return await client.CreateCheckStatusResponseAsync(req, instanceId);
    }
}
