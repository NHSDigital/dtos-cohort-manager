namespace NHS.CohortManager.ParticipantManagementServices;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.DurableTask.Client;
using Microsoft.DurableTask;
using System.Threading.Tasks;
using System.Collections.Generic;

public class ManageParticipant
{
    /// <summary>
    /// This function is triggered by a message on the "start-orchestration" Service Bus queue.
    /// It starts a new durable orchestration instance.
    /// </summary>
    /// <param name="messageBody">The content of the Service Bus message.</param>
    /// <param name="durableClient">The durable task client used to start new orchestrations.</param>
    /// <param name="functionContext">The function execution context.</param>
    [Function("ServiceBusQueueTrigger")]
    public static async Task Run(
        [ServiceBusTrigger("start-orchestration", Connection = "ServiceBusConnection")] string messageBody,
        [DurableClient] DurableTaskClient durableClient,
        FunctionContext functionContext)
    {
        var logger = functionContext.GetLogger<ManageParticipant>();
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
        var logger = context.CreateReplaySafeLogger<ManageParticipant>();
        string input = context.GetInput<string>();
        var outputs = new List<string>();

        logger.LogInformation("Orchestration started with input: {input}", input);

        try
        {
            // Chain of activities
            string data = await context.CallActivityAsync<string>(nameof(FetchDataActivity), input);
            outputs.Add($"Fetched: {data}");
            logger.LogInformation("FetchDataActivity completed.");

            string processedData = await context.CallActivityAsync<string>(nameof(ProcessDataActivity), data);
            outputs.Add($"Processed: {processedData}");
            logger.LogInformation("ProcessDataActivity completed.");

            string finalResult = await context.CallActivityAsync<string>(nameof(SaveDataActivity), processedData);
            outputs.Add(finalResult);
            logger.LogInformation("SaveDataActivity completed.");
        }
        catch (System.Exception ex)
        {
            logger.LogError(ex, "Orchestration failed with an exception.");
            // Handle exceptions, maybe call a cleanup activity
            throw;
        }


        return outputs;
    }

    /// <summary>
    /// Activity 1: Simulates fetching data from an external source.
    /// </summary>
    /// <param name="initialData">The input data for the activity.</param>
    /// <param name="functionContext">Function execution context.</param>
    /// <returns>Simulated fetched data as a string.</returns>
    [Function(nameof(FetchDataActivity))]
    public static string FetchDataActivity([ActivityTrigger] string initialData, FunctionContext functionContext)
    {
        var logger = functionContext.GetLogger<ManageParticipant>();
        logger.LogInformation("Executing FetchDataActivity with data: {initialData}", initialData);

        // Simulate some work
        Task.Delay(1000).Wait();

        return $"Data for '{initialData}' from external source";
    }

    /// <summary>
    /// Activity 2: Simulates processing the fetched data.
    /// </summary>
    /// <param name="dataToProcess">The data to be processed.</param>
    /// <param name="functionContext">Function execution context.</param>
    /// <returns>Processed data as a string.</returns>
    [Function(nameof(ProcessDataActivity))]
    public static string ProcessDataActivity([ActivityTrigger] string dataToProcess, FunctionContext functionContext)
    {
        var logger = functionContext.GetLogger<ManageParticipant>();
        logger.LogInformation("Executing ProcessDataActivity with data: {dataToProcess}", dataToProcess);

        // Simulate processing
        Task.Delay(1000).Wait();

        return dataToProcess.ToUpper();
    }

    /// <summary>
    /// Activity 3: Simulates saving the processed data.
    /// </summary>
    /// <param name="dataToSave">The data to be saved.</param>
    /// <param name="functionContext">Function execution context.</param>
    /// <returns>A confirmation message as a string.</returns>
    [Function(nameof(SaveDataActivity))]
    public static string SaveDataActivity([ActivityTrigger] string dataToSave, FunctionContext functionContext)
    {
        var logger = functionContext.GetLogger<ManageParticipant>();
        logger.LogInformation("Executing SaveDataActivity with data: {dataToSave}", dataToSave);

        // Simulate I/O operation
        Task.Delay(1000).Wait();

        return $"Successfully saved: {dataToSave}";
    }
}