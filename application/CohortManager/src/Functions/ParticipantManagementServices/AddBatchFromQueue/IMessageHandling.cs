namespace AddBatchFromQueue;

using Azure.Messaging.ServiceBus;

public interface IMessageHandling
{
    Task MessageHandler(ProcessMessageEventArgs args);
    Task ErrorHandler(ProcessErrorEventArgs args);
    Task CleanUpDeferredMessages(ServiceBusReceiver receiver, string queueName, string connectionString);

}