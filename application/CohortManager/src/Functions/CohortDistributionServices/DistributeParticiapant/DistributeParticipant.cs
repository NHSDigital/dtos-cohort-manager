namespace NHS.CohortManager.CohortDistributionServices;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.DurableTask.Client;
using Microsoft.DurableTask;
using System.Threading.Tasks;
using System.Collections.Generic;

public class DistributeCohort
{
    [Function("ServiceBusQueueTrigger")]
    public static async Task Run(
        [ServiceBusTrigger("start-orchestration", Connection = "ServiceBusConnection")] string messageBody,
        [DurableClient] DurableTaskClient durableClient,
        FunctionContext functionContext)
    {
        var logger = functionContext.GetLogger<DistributeCohort>();
        logger.LogInformation($"Received message: {messageBody}. Starting new orchestration.");

        // Start a new orchestration instance and pass the message body as input.
        string instanceId = await durableClient.ScheduleNewOrchestrationInstanceAsync(
            nameof(MyOrchestration), messageBody);

        logger.LogInformation("Started orchestration with ID = '{instanceId}'.", instanceId);
    }

    [Function(nameof(MyOrchestration))]
    public static async Task<List<string>> MyOrchestration(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var logger = context.CreateReplaySafeLogger<DistributeCohort>();
        string input = context.GetInput<string>();
        var outputs = new List<string>();

        logger.LogInformation("Orchestration started with input: {input}", input);

        try
        {
        }
        catch (System.Exception ex)
        {
            logger.LogError(ex, "Orchestration failed with an exception.");
            throw;
        }


        return outputs;
    }

    // [Function(nameof(FetchDataActivity))]
    // public static string FetchDataActivity([ActivityTrigger] string initialData, FunctionContext functionContext)
    // {
    //     return string.Empty;
    // }

}