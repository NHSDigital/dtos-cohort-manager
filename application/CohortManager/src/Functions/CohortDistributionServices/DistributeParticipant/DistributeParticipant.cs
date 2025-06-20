namespace NHS.CohortManager.CohortDistributionServices;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.DurableTask.Client;
using Microsoft.DurableTask;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Options;

public class DistributeParticipant
{
    private readonly ILogger<DistributeParticipant> _logger;
    private readonly DistributeParticipantConfig _config;

    public DistributeParticipant(ILogger<DistributeParticipant> logger, IOptions<DistributeParticipantConfig> config)
    {
        _logger = logger;
        _config = config.Value;
    }

    [Function(nameof(MyOrchestration))]
    public async Task<List<string>> MyOrchestration(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        string input = context.GetInput<string>();
        var outputs = new List<string>();

        _logger.LogInformation("Orchestration started with input: {input}", input);

        try
        {
            var task = context.CallActivityAsync<bool>(
                               nameof(FetchDataActivity));
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Orchestration failed with an exception.");
            throw;
        }


        return outputs;
    }

    [Function(nameof(FetchDataActivity))]
    public static string FetchDataActivity([ActivityTrigger] string initialData, FunctionContext functionContext)
    {
        return string.Empty;
    }


    [Function("ServiceBusQueueTrigger")]
    public async Task Run(
       [ServiceBusTrigger("%CohortQueueName%", Connection = "ServiceBusConnectionString")] string messageBody,
       [DurableClient] DurableTaskClient durableClient,
       FunctionContext functionContext)
    {
        _logger.LogInformation($"Received message: {messageBody}");

        // Start a new orchestration instance and pass the message body as input.
        string instanceId = await durableClient.ScheduleNewOrchestrationInstanceAsync(
            nameof(MyOrchestration), messageBody);

        _logger.LogInformation("Started orchestration with ID = '{instanceId}'.", instanceId);
    }


}