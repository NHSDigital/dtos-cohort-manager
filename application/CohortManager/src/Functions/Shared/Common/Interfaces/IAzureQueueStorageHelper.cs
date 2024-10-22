namespace Common;

using Azure.Storage.Queues;

public interface IAzureQueueStorageHelper
{
    Task<QueueClient> AddItemsToQueueAsync(string queueName, string storageAccountName);
}