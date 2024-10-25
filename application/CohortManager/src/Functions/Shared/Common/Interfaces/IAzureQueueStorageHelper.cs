namespace Common;

using Azure.Storage.Queues;
using Model;

public interface IAzureQueueStorageHelper
{
    Task AddItemsToQueueAsync<T>(T participantCsvRecord);
}
