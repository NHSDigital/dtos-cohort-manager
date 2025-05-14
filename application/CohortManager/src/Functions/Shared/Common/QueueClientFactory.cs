namespace Common;
using Azure.Storage.Queues;


public class QueueClientFactory : IQueueClientFactory
{
    private readonly string _storageConnectionString;

    public QueueClientFactory()
    {
        _storageConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
    }

    public QueueClient CreateClient(string queueName)
    {
        return new QueueClient(_storageConnectionString, queueName);
    }
}
