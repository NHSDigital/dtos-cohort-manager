namespace Common;

using Azure.Storage.Queues;
using Model;

public interface IAzureQueueStorageHelper
{
    Task AddItemToQueueAsync<T>(T participantCsvRecord);
}
