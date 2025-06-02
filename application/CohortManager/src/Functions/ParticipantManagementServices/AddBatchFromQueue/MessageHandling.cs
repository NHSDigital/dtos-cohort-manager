namespace AddBatchFromQueue;

using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Castle.Core.Logging;
using Microsoft.Extensions.Logging;
using Model;

public class MessageHandling : IMessageHandling
{
    private readonly IMessageStore _messageStore;
    private readonly ILogger<MessageHandling> _logger;
    private readonly static object lockObj = new object();
    public MessageHandling(IMessageStore messageStore, ILogger<MessageHandling> logger)
    {
        _messageStore = messageStore;
        _logger = logger;
    }

    public async Task MessageHandler(ProcessMessageEventArgs args)
    {
        var message = new SerializableMessage()
        {
            MessageId = args.Message.MessageId,
            Body = args.Message.Body.ToString(),
            Subject = args.Message.Subject,
            EnqueuedTime = args.Message.EnqueuedTime,
            SequenceNumber = args.Message.SequenceNumber,
            IsCompleted = false
        };

        lock (lockObj)
        {
            _messageStore.ListOfAllValues.Add(message);
            // could set a time threshold instead and not defer the messages on the queue
            if (_messageStore.ListOfAllValues.Count >= _messageStore.ExpectedMessageCount)
            {
                _messageStore.AllMessagesReceived.TrySetResult(true);
            }
        }
        _logger.LogWarning("adding message to in memory list", args.Message.SequenceNumber);
        await args.DeferMessageAsync(args.Message);
    }

    // handle any errors when receiving messages
    public Task ErrorHandler(ProcessErrorEventArgs args)
    {
        _logger.LogError(args.Exception, args.Exception.Message);
        return Task.CompletedTask;
    }

    public async Task CleanUpMessages(ServiceBusReceiver receiver, string queueName, string connectionString)
    {
        try
        {
            _logger.LogWarning($"cleaning up messages started at: {DateTime.Now}");
            foreach (var message in _messageStore.ListOfAllValues)
            {
                if (message.IsCompleted)
                {
                    continue;
                }

                ServiceBusReceivedMessage peeked = await receiver.PeekMessageAsync(message.SequenceNumber);
                if (peeked == null)
                {
                    continue;
                }

                if (peeked.State == ServiceBusMessageState.Deferred)
                {
                    // Try to receive it by sequence number
                    var deferredMessage = await receiver.ReceiveDeferredMessageAsync(peeked.SequenceNumber);
                    if (deferredMessage != null)
                    {
                        var messageBody = JsonSerializer.Deserialize<BasicParticipantCsvRecord>(deferredMessage.Body);
                        if (messageBody != null || messageBody.retryCount < 5)
                        {
                            messageBody.retryCount++;
                            await receiver.CompleteMessageAsync(deferredMessage);
                            await SendMessage(connectionString, queueName, JsonSerializer.Serialize(messageBody));
                        }
                        else
                        {
                            await receiver.DeadLetterMessageAsync(deferredMessage, "Manually dead-lettering deferred msg");
                            _logger.LogWarning($"Dead-lettered deferred message with Seq #{deferredMessage.SequenceNumber}");
                        }
                    }
                }
            }
            _logger.LogWarning($"cleaning up messages finished: {DateTime.Now}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
        }
    }
    public async Task CleanUpDeferredMessages(ServiceBusReceiver receiver, string queueName, string connectionString)
    {
        var adminClient = new ServiceBusAdministrationClient(connectionString);
        var runtimeProperties = await adminClient.GetQueueRuntimePropertiesAsync(queueName);

        try
        {
            _logger.LogWarning($"GetMessagesFromQueueActivity started at: {DateTime.Now}");
            var messagesInQueue = runtimeProperties.Value.ActiveMessageCount;

            for (int i = 0; i < messagesInQueue; i++)
            {
                ServiceBusReceivedMessage serviceBusMessage = await receiver.PeekMessageAsync();
                if (serviceBusMessage == null)
                {
                    return;
                }

                if (serviceBusMessage != null)
                {
                    if (serviceBusMessage.State == ServiceBusMessageState.Deferred)
                    {
                        var fullMessage = await receiver.ReceiveDeferredMessageAsync(serviceBusMessage.SequenceNumber);
                        await receiver.CompleteMessageAsync(fullMessage);

                        //await receiver.DeadLetterMessageAsync(serviceBusMessage, "Manually dead-lettering deferred msg");
                        _logger.LogWarning($"Dead-lettered deferred message with Seq #{serviceBusMessage.SequenceNumber}");

                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
        }
    }



    private async Task SendMessage(string queueConnectionString, string queueName, string record)
    {
        await using var client = new ServiceBusClient(queueConnectionString);
        ServiceBusSender sender = client.CreateSender(queueName);

        // Create a message batch
        using ServiceBusMessageBatch messageBatch = await sender.CreateMessageBatchAsync();

        ServiceBusMessage message = new ServiceBusMessage(record);
        if (!messageBatch.TryAddMessage(message))
        {
            throw new Exception($"The message is too large to fit in the batch.");
        }

        await sender.SendMessagesAsync(messageBatch);
        _logger.LogWarning("sent message back to queue");

    }
}
