namespace Common;

using Azure.Storage.Queues;
using Model;

public interface IAzureQueueStorageHelper
{
    Task AddItemsToQueueAsync(string queueName, string storageAccountName, List<BasicParticipantCsvRecord> participantCsvRecords);
}