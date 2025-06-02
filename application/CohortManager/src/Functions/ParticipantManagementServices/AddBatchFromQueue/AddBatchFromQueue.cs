namespace AddBatchFromQueue;

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using DataServices.Client;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using Model;

public class AddBatchFromQueue
{
    private readonly ILogger _logger;
    private readonly IDataServiceClient<ParticipantManagement> _participantManagementClient;
    private readonly IAddBatchFromQueueHelper _addBatchFromQueueHelper;
    private readonly IMessageStore _messageStore;
    private readonly IMessageHandling _messageHandling;
    private readonly string connectionString;
    private readonly string queueName;


    public AddBatchFromQueue(
        ILoggerFactory loggerFactory,
        IDataServiceClient<ParticipantManagement> participantManagementClient,
        IAddBatchFromQueueHelper addBatchFromQueueHelper,
        IMessageHandling messageHandling,
        IMessageStore messageStore
        )
    {
        _logger = loggerFactory.CreateLogger<AddBatchFromQueue>();
        _participantManagementClient = participantManagementClient;
        _addBatchFromQueueHelper = addBatchFromQueueHelper;
        _messageHandling = messageHandling;
        _messageStore = messageStore;

        connectionString = Environment.GetEnvironmentVariable("QueueConnectionString")!;
        queueName = Environment.GetEnvironmentVariable("QueueName")!;

        _logger.LogWarning(System.Diagnostics.Process.GetCurrentProcess().Id.ToString());
    }

    [Function(nameof(AddBatchFromQueueOrchestrator))]
    public async Task AddBatchFromQueueOrchestrator(
       [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var client = new ServiceBusClient(connectionString);
        var receiver = client.CreateReceiver(queueName);

        var participants = new List<ParticipantManagement>();
        var participantsData = new List<ParticipantCsvRecord>();

        try
        {
            _logger.LogInformation($"DrainServiceBusQueue started at: {DateTime.Now}");

            var ListOfAllValuesFromQueue = await context.CallActivityAsync<List<SerializableMessage>>(
                  nameof(GetMessagesFromQueueActivity)
              );

            if (ListOfAllValuesFromQueue.Count == 0)
            {
                _logger.LogWarning("no items to process from the queue Activity or in store");
                return;
            }

            var chunks = ListOfAllValuesFromQueue.Chunk(500).ToList();
            // process in batches of chunk size provided
            foreach (var chunk in chunks)
            {
                var participantsWithDetails = await context.CallActivityAsync<List<SerializableMessage>>(nameof(GetDemoGraphicData), chunk);

                var ValidatedParticipantCsvRecords = await context.CallActivityAsync<List<SerializableMessage>>(nameof(ValidateMessage), participantsWithDetails!);

                var participantsToAddToDatabase = await context.CallActivityAsync<participantObjectList>(nameof(ProcessChunk), ValidatedParticipantCsvRecords!);


                //add current batch to database and cohort queue  process current batch 
                var tasks = new List<Task>()
                {
                    context.CallActivityAsync<bool>(nameof(AddItemsToDatabase), participantsToAddToDatabase.ParticipantManagementRecords),
                    context.CallActivityAsync<bool>(nameof(AddItemsToCohortQueue), participantsToAddToDatabase.ParticipantCsvRecords)
                };


                await Task.WhenAll(tasks);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
        }
        finally
        {
            // when the batch is finished processing we need to make sure we can start a new batch form the service buss queue
            //await _messageHandling.CleanUpMessages(receiver, queueName, connectionString);
            _messageStore.processingCurrentBatch = false;
            if (_messageStore.ListOfAllValues != null)
            {
                _messageStore.ListOfAllValues.Clear();
            }
            await _messageHandling.CleanUpDeferredMessages(receiver, queueName, connectionString);
            await receiver.CloseAsync();
            await client.DisposeAsync();
        }
    }

    [Function(nameof(ProcessChunk))]
    public async Task<participantObjectList> ProcessChunk([ActivityTrigger] List<SerializableMessage> ValidatedParticipantCsvRecords)
    {
        var client = new ServiceBusClient(connectionString);
        var receiver = client.CreateReceiver(queueName);

        try
        {
            var participantsData = new ConcurrentQueue<ParticipantCsvRecord>();
            var participants = new ConcurrentQueue<ParticipantManagement>();

            var options = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount };
            await Parallel.ForEachAsync(ValidatedParticipantCsvRecords, options, async (messageFromQueue, cancellationToken) =>
            {
                var fullMessage = await receiver.ReceiveDeferredMessageAsync(messageFromQueue.SequenceNumber);

                var validatedMessageFromQueue = JsonSerializer.Deserialize<ParticipantCsvRecord>(messageFromQueue.Body);
                participantsData.Enqueue(validatedMessageFromQueue!);
                participants.Enqueue(validatedMessageFromQueue!.Participant.ToParticipantManagement());


                _logger.LogWarning($"now completing message with sequence number {messageFromQueue.SequenceNumber}");
                await receiver.CompleteMessageAsync(fullMessage);
                _messageStore.ListOfAllValues.Remove(messageFromQueue);
            });

            return new participantObjectList()
            {
                ParticipantCsvRecords = participantsData.ToList(),
                ParticipantManagementRecords = participants.ToList()
            };
        }
        finally
        {
            await client.DisposeAsync();
            await receiver.DisposeAsync();
        }
    }

    [Function(nameof(GetMessagesFromQueueActivity))]
    public async Task<List<SerializableMessage>> GetMessagesFromQueueActivity([ActivityTrigger] FunctionContext functionContext)
    {
        var serviceBusProcessorOptions = new ServiceBusProcessorOptions()
        {
            ReceiveMode = ServiceBusReceiveMode.PeekLock,
            AutoCompleteMessages = false,
            PrefetchCount = 0,
            MaxConcurrentCalls = 1,
        };
        var client = new ServiceBusClient(connectionString);
        var receiver = client.CreateReceiver(queueName);
        var processor = client.CreateProcessor(queueName, serviceBusProcessorOptions);
        var adminClient = new ServiceBusAdministrationClient(connectionString);
        var runtimeProperties = await adminClient.GetQueueRuntimePropertiesAsync(queueName);

        try
        {
            _logger.LogWarning($"GetMessagesFromQueueActivity started at: {DateTime.Now}");

            var itemsOnQueue = runtimeProperties.Value.ActiveMessageCount;
            if (itemsOnQueue == 0)
            {
                _logger.LogWarning("nothing to process");
                await processor.StopProcessingAsync();
                return new List<SerializableMessage>();
            }

            //get a batch from the service buss queue
            _messageStore.ExpectedMessageCount = runtimeProperties.Value.ActiveMessageCount;


            _messageStore.ListOfAllValues = new List<SerializableMessage>();
            _messageStore.AllMessagesReceived = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            processor.ProcessMessageAsync += _messageHandling.MessageHandler;
            processor.ProcessErrorAsync += _messageHandling.ErrorHandler;

            await processor.StartProcessingAsync();
            _messageStore.processingCurrentBatch = true;


            // Wait until all messages received or timeout
            var timeout = Task.Delay(TimeSpan.FromMinutes(5));
            var completed = await Task.WhenAny(_messageStore.AllMessagesReceived.Task, timeout);

            if (completed == _messageStore.AllMessagesReceived.Task)
            {
                await processor.StopProcessingAsync();
                return _messageStore.ListOfAllValues;
            }
            else
            {
                _logger.LogWarning("Timed out before all messages received. processing what is in current batch");
                return _messageStore.ListOfAllValues;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return new List<SerializableMessage>();
        }
        finally
        {
            // Calling DisposeAsync on client types is required to ensure that network
            // resources and other unmanaged objects are properly cleaned up.
            await receiver.CloseAsync();
            await processor.DisposeAsync();
            await client.DisposeAsync();
        }
    }

    [Function(nameof(GetDemoGraphicData))]
    public async Task<List<SerializableMessage>?> GetDemoGraphicData([ActivityTrigger] List<SerializableMessage> serializableMessages)
    {
        return await _addBatchFromQueueHelper.GetDemoGraphicData(serializableMessages);
    }

    [Function(nameof(ValidateMessage))]
    public async Task<List<SerializableMessage>?> ValidateMessage([ActivityTrigger] List<SerializableMessage> serializableMessages)
    {
        return await _addBatchFromQueueHelper.ValidateMessageFromQueue(serializableMessages);
    }


    [Function(nameof(AddItemsToDatabase))]
    public async Task<bool> AddItemsToDatabase([ActivityTrigger] List<ParticipantManagement> participants)
    {
        if (participants.Any())
        {
            _logger.LogWarning($"sent messages from the queue to database");
            return await _participantManagementClient.AddRange(participants); ;
        }
        _logger.LogWarning($"no participants to add");
        return false;
    }

    [Function(nameof(AddItemsToCohortQueue))]
    public async Task<bool> AddItemsToCohortQueue([ActivityTrigger] List<ParticipantCsvRecord> participants)
    {
        if (participants.Any())
        {
            await _addBatchFromQueueHelper.AddAllCohortRecordsToQueue(participants);
            _logger.LogWarning($"sent messages from the queue to cohort");

            return true;
        }

        return false;
    }

    [Function("add_batch_timer_start")]
    public async Task AddBatchFromQueue_start(
      [TimerTrigger("* * * * *")] TimerInfo myTimer,
      [DurableClient(TaskHub = "BatchProcessorTaskHub")] DurableTaskClient client)
    {
        _logger.LogWarning("add_batch_timer_start function triggered");
        // if there is not a batch currently processing then schedule a new batch to run
        if (!_messageStore.processingCurrentBatch)
        {
            var instanceId = await client.ScheduleNewOrchestrationInstanceAsync(nameof(AddBatchFromQueueOrchestrator));
        }
        else
        {
            _logger.LogWarning("no new Scheduled Orchestration Instance created because an old batch is currently processing");
        }
    }
}


