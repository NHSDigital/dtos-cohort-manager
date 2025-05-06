namespace Common;

using Azure.Storage.Queues;
using Model;

public interface IAzureQueueStorageHelper
{
    Task<bool> AddItemToQueueAsync<T>(T participantCsvRecord, string queueName);

    Task<int> GetNumberOfItemsInQueue(string queueName);
}
