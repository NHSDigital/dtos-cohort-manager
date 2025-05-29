namespace AddBatchFromQueue;

using System;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using DataServices.Client;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using Model;
using OpenTelemetry.Trace;

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

            int processedRecords = 0;
            foreach (var message in ListOfAllValuesFromQueue)
            {
                if (processedRecords == 5)
                {
                    await context.CallActivityAsync(
                        nameof(AlwaysFails)
                    );
                    Console.WriteLine("yes");
                }

                try
                {
                    string jsonFromQueue = message.Body;
                    _logger.LogInformation($"Processing message: {jsonFromQueue}");

                    var participantCsvRecord = await context.CallActivityAsync<ParticipantCsvRecord>(
                       nameof(GetDemoGraphicData), jsonFromQueue);

                    var ValidatedParticipantCsvRecord = await context.CallActivityAsync<ParticipantCsvRecord>(
                       nameof(ValidateMessage), participantCsvRecord!);

                    if (ValidatedParticipantCsvRecord == null)
                    {
                        _logger.LogError("The result of validating a record was null, see errors in database for more details. Will still process any other records");
                        // this sends a record to the dead letter queue on error
                        await context.CallActivityAsync(nameof(DeadLetterItemAsync), message.SequenceNumber);
                    }
                    // we only want to add non null items to the database and log error records to the database this handled by validation 
                    else
                    {
                        participantsData.Add(ValidatedParticipantCsvRecord!);
                        participants.Add(ValidatedParticipantCsvRecord!.Participant.ToParticipantManagement());

                        await context.CallActivityAsync(nameof(completeMessageAsync), message.SequenceNumber);

                        var messageFromQueue = _messageStore.ListOfAllValues.Where(x => x.MessageId == message.MessageId).FirstOrDefault();
                        messageFromQueue.IsCompleted = true;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process message");
                    // send error messages to the dead letter queue
                    await context.CallActivityAsync(nameof(DeadLetterItemAsync), message.SequenceNumber);
                }
                processedRecords++;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
        }
        finally
        {
            var addAllRecordsToCohortQueue = false;
            if (participants.Any())
            {
                addAllRecordsToCohortQueue = await context.CallActivityAsync<bool>(
                    nameof(AddItemsToDatabase), participants
                );
            }

            if (addAllRecordsToCohortQueue && participantsData.Any())
            {
                await context.CallActivityAsync<bool>(
                    nameof(AddItemsToCohortQueue), participantsData
                );
            }

            // dispose of all lists and variables from memory because they are no longer needed
            if (_messageStore.ListOfAllValues != null)
            {
                await _messageHandling.CleanUpMessages(receiver, queueName, connectionString);
                _messageStore.ListOfAllValues.Clear();
            }
            await receiver.CloseAsync();
            await client.DisposeAsync();
        }
    }

    [Function(nameof(AlwaysFails))]
    public void AlwaysFails([ActivityTrigger] FunctionContext functionContext)
    {
        throw new Exception("testing always fails retry");
    }

    [Function(nameof(GetMessagesFromQueueActivity))]
    public async Task<List<SerializableMessage>> GetMessagesFromQueueActivity([ActivityTrigger] FunctionContext functionContext)
    {
        var serviceBusProcessorOptions = new ServiceBusProcessorOptions()
        {
            ReceiveMode = ServiceBusReceiveMode.PeekLock,
            MaxAutoLockRenewalDuration = TimeSpan.FromMinutes(5) // lock message for 5 mins

        };
        var client = new ServiceBusClient(connectionString);
        var receiver = client.CreateReceiver(queueName);
        var processor = client.CreateProcessor(queueName, serviceBusProcessorOptions);
        var adminClient = new ServiceBusAdministrationClient(connectionString);
        var runtimeProperties = await adminClient.GetQueueRuntimePropertiesAsync(queueName);

        try
        {
            _logger.LogWarning($"GetMessagesFromQueueActivity started at: {DateTime.Now}");
            _messageStore.ExpectedMessageCount = runtimeProperties.Value.ActiveMessageCount;

            if (_messageStore.ExpectedMessageCount == 0)
            {
                _logger.LogWarning("nothing to process");
                await processor.StopProcessingAsync();
                return new List<SerializableMessage>();
            }

            _messageStore.ListOfAllValues = new List<SerializableMessage>();
            _messageStore.AllMessagesReceived = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            processor.ProcessMessageAsync += _messageHandling.MessageHandler;
            processor.ProcessErrorAsync += _messageHandling.ErrorHandler;

            await processor.StartProcessingAsync();

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
            // Calling DisposeAsync on client types is required to ensure that network
            // resources and other unmanaged objects are properly cleaned up.
            await receiver.CloseAsync();
            await processor.DisposeAsync();
            await client.DisposeAsync();
        }
    }

    [Function(nameof(GetDemoGraphicData))]
    public async Task<ParticipantCsvRecord?> GetDemoGraphicData([ActivityTrigger] string jsonFromQueue)
    {
        return await _addBatchFromQueueHelper.GetDemoGraphicData(jsonFromQueue);
    }

    [Function(nameof(ValidateMessage))]
    public async Task<ParticipantCsvRecord?> ValidateMessage([ActivityTrigger] ParticipantCsvRecord participantCsvRecord)
    {
        return await _addBatchFromQueueHelper.ValidateMessageFromQueue(participantCsvRecord);
    }


    [Function(nameof(AddItemsToDatabase))]
    public async Task<bool> AddItemsToDatabase([ActivityTrigger] List<ParticipantManagement> participants)
    {
        _logger.LogWarning($"sent messages from the queue to database");
        return await _participantManagementClient.AddRange(participants); ;
    }

    [Function(nameof(AddItemsToCohortQueue))]
    public async Task<bool> AddItemsToCohortQueue([ActivityTrigger] List<ParticipantCsvRecord> participants)
    {
        await _addBatchFromQueueHelper.AddAllCohortRecordsToQueue(participants);
        _logger.LogWarning($"sent messages from the queue to cohort");

        return true;
    }

    [Function(nameof(DeadLetterItemAsync))]
    public async Task DeadLetterItemAsync([ActivityTrigger] long sequenceNumber)
    {
        var client = new ServiceBusClient(connectionString);
        var receiver = client.CreateReceiver(queueName);
        try
        {
            var fullMessage = await receiver.ReceiveDeferredMessageAsync(sequenceNumber);
            await receiver.DeadLetterMessageAsync(fullMessage);
        }
        finally
        {
            await client.DisposeAsync();
            await receiver.DisposeAsync();
        }
    }

    [Function(nameof(completeMessageAsync))]
    public async Task completeMessageAsync([ActivityTrigger] long sequenceNumber)
    {
        var client = new ServiceBusClient(connectionString);
        var receiver = client.CreateReceiver(queueName);

        try
        {
            var fullMessage = await receiver.ReceiveDeferredMessageAsync(sequenceNumber);
            await receiver.CompleteMessageAsync(fullMessage);
        }
        finally
        {
            await client.DisposeAsync();
            await receiver.DisposeAsync();
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


