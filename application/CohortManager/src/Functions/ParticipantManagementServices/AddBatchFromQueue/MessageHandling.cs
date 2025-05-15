namespace AddBatchFromQueue;

using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Castle.Core.Logging;
using Microsoft.Extensions.Logging;

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
            SequenceNumber = args.Message.SequenceNumber
        };

        lock (lockObj)
        {
            _messageStore.ListOfAllValues.Add(message);

            if (_messageStore.ListOfAllValues.Count >= _messageStore.ExpectedMessageCount)
            {
                _messageStore.AllMessagesReceived.TrySetResult(true);
            }
        }
        await args.DeferMessageAsync(args.Message);
    }

    // handle any errors when receiving messages
    public Task ErrorHandler(ProcessErrorEventArgs args)
    {
        return Task.CompletedTask;
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
                ServiceBusReceivedMessage peeked = await receiver.PeekMessageAsync();
                if (peeked == null)
                {
                    return;
                }

                if (peeked.State == ServiceBusMessageState.Deferred)
                {
                    // Try to receive it by sequence number
                    var deferredMessage = await receiver.ReceiveDeferredMessageAsync(peeked.SequenceNumber);
                    if (deferredMessage != null)
                    {
                        await receiver.DeadLetterMessageAsync(deferredMessage, "Manually dead-lettering deferred msg");
                        _logger.LogWarning($"Dead-lettered deferred message with Seq #{peeked.SequenceNumber}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
        }
    }
}
