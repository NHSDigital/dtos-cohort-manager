namespace addParticipant;

using System.ComponentModel;
using System.IO.Pipelines;
using Azure.Messaging.ServiceBus;
using Common;
using DurableTask.Core;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NHS.Screening.DemographicDurableFunction;

public class DurableAddFunction
{
    private const int batchSize = 10;
    private List<string> batch = new List<string>();
    private readonly DurableAddFunctionConfig _config;
    private readonly IAzureQueueStorageHelper _azureQueueStorageHelper;

    private readonly ILogger<DurableAddFunction> _logger;

    public DurableAddFunction(IOptions<DurableAddFunctionConfig> config, IAzureQueueStorageHelper azureQueueStorageHelper, ILogger<DurableAddFunction> logger)
    {
        _azureQueueStorageHelper = azureQueueStorageHelper;
        _config = config.Value;
        _logger = logger;
    }

    [Function(nameof(DurableAddFunction))]
    public async Task RunDurableAddOrchestrator(
       [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        try
        {
            var AddJsonData = context.GetInput<string>();
            var itemsInQueue = await context.CallActivityAsync<int>(
                      nameof(GetNumberOfItemsInQueue),
                      _config.AddQueueName
                  );

            batch.Add(AddJsonData);

            if (batch.Count >= batchSize)
            {
                var task = await context.CallActivityAsync<bool>(
                      nameof(SendBatch),
                      batch
                  );
            }
            else if (itemsInQueue == 0)
            {
                var task = await context.CallActivityAsync<bool>(
                      nameof(SendBatch),
                      batch
                  );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
        }

    }

    [Function(nameof(GetNumberOfItemsInQueue))]
    public async Task<int> GetNumberOfItemsInQueue([ActivityTrigger] string AddQueueName)
    {
        return await _azureQueueStorageHelper.GetNumberOfItemsInQueue(AddQueueName);
    }

    [Function(nameof(SendBatch))]
    public async Task<bool> SendBatch([ActivityTrigger] List<string> records, FunctionContext executionContext)
    {
        await using var client = new ServiceBusClient(_config.QueueConnectionString);
        ServiceBusSender sender = client.CreateSender(_config.QueueName);

        // Create a message batch
        using ServiceBusMessageBatch messageBatch = await sender.CreateMessageBatchAsync();

        foreach (string record in records)
        {
            ServiceBusMessage message = new ServiceBusMessage(record);

            // Try to add the message to the batch
            if (!messageBatch.TryAddMessage(message))
            {
                throw new Exception($"The message is too large to fit in the batch.");
            }
        }

        // Send the batch to the Service Bus queue
        await sender.SendMessagesAsync(messageBatch);
        batch.Clear();
        return true;
    }

    [Function("AddQueueItemToEntity")]
    public async Task Run(
    [QueueTrigger("add-participant-queue")] string queueItem,
    [DurableClient] DurableTaskClient client)
    {
        var instanceId = await client.ScheduleNewOrchestrationInstanceAsync(
            nameof(DurableAddFunction), queueItem, CancellationToken.None);
    }
}

