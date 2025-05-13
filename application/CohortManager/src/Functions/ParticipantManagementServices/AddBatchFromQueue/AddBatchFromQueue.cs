namespace AddBatchFromQueue;

using System;
using System.Net.WebSockets;
using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Common;
using DataServices.Client;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Model;

public class AddBatchFromQueue
{
    private readonly ILogger _logger;
    private readonly IDataServiceClient<ParticipantManagement> _participantManagementClient;
    private readonly IAddBatchFromQueueProcessHelper _addBatchFromQueueProcessHelper;
    private IMessageHandling _messageHandling;

    private readonly AddBatchFromQueueConfig _config;

    private int totalMessages;
    private int batchSize;
    private readonly string connectionString;
    private readonly string queueName;
    public static long _expectedMessageCount = 0;

    public static List<SerializableMessage> listOfAllValues;
    public static TaskCompletionSource<bool> _allMessagesReceived;

    public AddBatchFromQueue(
        ILoggerFactory loggerFactory,
        IDataServiceClient<ParticipantManagement> participantManagementClient,

        IOptions<AddBatchFromQueueConfig> config,
        IMessageHandling messageHandling,
        IAddBatchFromQueueProcessHelper addBatchFromQueueProcessHelper
        )
    {
        _logger = loggerFactory.CreateLogger<AddBatchFromQueue>();
        _participantManagementClient = participantManagementClient;

        _messageHandling = messageHandling;
        _addBatchFromQueueProcessHelper = addBatchFromQueueProcessHelper;

        _config = config.Value;

        totalMessages = 0;
        batchSize = int.Parse(_config.AddBatchSize);
        listOfAllValues = new List<SerializableMessage>();

        connectionString = Environment.GetEnvironmentVariable("QueueConnectionString")!;
        queueName = Environment.GetEnvironmentVariable("QueueName")!;
    }

    [Function(nameof(AddBatchFromQueueOrchestrator))]
    public async Task AddBatchFromQueueOrchestrator(
       [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        try
        {
            var listOfAllValues = await context.CallActivityAsync<List<SerializableMessage>>(
                  nameof(GetMessagesFromQueueActivity),
                  queueName
              );

            if (listOfAllValues.Count != 0)
            {
                var ProcessItemsActivityTask = await context.CallActivityAsync<bool>(
                    nameof(ProcessItemsActivity), listOfAllValues
                );
            }
            _logger.LogWarning("no items to process from the ProcessItems Activity");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
        }
    }

    [Function(nameof(GetMessagesFromQueueActivity))]
    public async Task<List<SerializableMessage>> GetMessagesFromQueueActivity([ActivityTrigger] FunctionContext functionContext)
    {
        var serviceBusProcessorOptions = new ServiceBusProcessorOptions()
        {
            ReceiveMode = ServiceBusReceiveMode.PeekLock
        };
        var client = new ServiceBusClient(connectionString);
        var receiver = client.CreateReceiver(queueName);

        var processor = client.CreateProcessor(queueName, serviceBusProcessorOptions);

        try
        {
            _logger.LogInformation($"DrainServiceBusQueue started at: {DateTime.Now}");

            var adminClient = new ServiceBusAdministrationClient(connectionString);
            var runtimeProperties = await adminClient.GetQueueRuntimePropertiesAsync(queueName);
            _expectedMessageCount = runtimeProperties.Value.ActiveMessageCount;

            if (_expectedMessageCount == 0)
            {
                _logger.LogInformation("nothing to process");
                await processor.StopProcessingAsync();
                return new List<SerializableMessage>();
            }
            _allMessagesReceived = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            listOfAllValues.Clear();

            processor.ProcessMessageAsync += _messageHandling.MessageHandler;
            processor.ProcessErrorAsync += _messageHandling.ErrorHandler;

            await processor.StartProcessingAsync();

            // Wait until all messages received or timeout
            var completed = await Task.WhenAny(_allMessagesReceived.Task);

            if (completed == _allMessagesReceived.Task)
            {
                await processor.StopProcessingAsync();
                return listOfAllValues;
            }
            else
            {
                _logger.LogWarning("Timed out before all messages received.");
                return new List<SerializableMessage>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return new List<SerializableMessage>();
        }
        finally
        {
            await receiver.CloseAsync();
            // Calling DisposeAsync on client types is required to ensure that network
            // resources and other unmanaged objects are properly cleaned up.
            await processor.DisposeAsync();
            await client.DisposeAsync();
        }
    }

    [Function(nameof(ProcessItemsActivity))]
    public async Task<bool> ProcessItemsActivity([ActivityTrigger] List<SerializableMessage> listOfAllValues)
    {
        var participants = new List<ParticipantManagement>();
        var participantsData = new List<ParticipantCsvRecord>();

        var allTasks = new List<Task>();
        var options = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount };

        _logger.LogInformation($"DrainServiceBusQueue started at: {DateTime.Now}");
        var serviceBusProcessorOptions = new ServiceBusProcessorOptions()
        {
            ReceiveMode = ServiceBusReceiveMode.PeekLock
        };
        var client = new ServiceBusClient(connectionString);
        var receiver = client.CreateReceiver(queueName);

        var processor = client.CreateProcessor(queueName, serviceBusProcessorOptions);

        //split list of all into N amount of chunks to be processed as batches.
        try
        {
            await _addBatchFromQueueProcessHelper.processItem(receiver, listOfAllValues, participantsData, participants, options, totalMessages);

            // add batch to database
            await _participantManagementClient.AddRange(participants);

            // add all items to queue
            await _addBatchFromQueueProcessHelper.AddAllCohortRecordsToQueue(participantsData);
            _logger.LogWarning($"Drained and processed {totalMessages} messages from the queue.");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return false;
        }
        finally
        {
            // dispose of all lists and variables from memory because they are no longer needed
            listOfAllValues.Clear();
            await receiver.CloseAsync();
            await processor.DisposeAsync();
            await client.DisposeAsync();
        }
    }


    [Function("add_batch_timer_start")]
    public async Task AddBatchFromQueue_start(
      [TimerTrigger("* * * * *")] TimerInfo myTimer,
      [DurableClient(TaskHub = "BatchProcessorTaskHub")] DurableTaskClient client)
    {
        string instanceId = await client.ScheduleNewOrchestrationInstanceAsync(nameof(AddBatchFromQueueOrchestrator));
    }
}


