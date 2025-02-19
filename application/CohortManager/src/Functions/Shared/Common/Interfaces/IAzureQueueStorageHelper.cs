namespace Common;

using Azure.Storage.Queues;
using Model;

public interface IAzureQueueStorageHelper
{
    Task<bool> AddItemToQueueAsync<T>(T participantCsvRecord, string queueName);
    Task<List<T>> GetItemsFromQueue<T>(int numberOfItems, string queueName) where T : class;
}
