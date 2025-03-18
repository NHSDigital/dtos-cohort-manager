namespace Common;

using Azure.Storage.Queues;

public interface IQueueClientFactory
{
    public QueueClient CreateClient(string queueName);
}