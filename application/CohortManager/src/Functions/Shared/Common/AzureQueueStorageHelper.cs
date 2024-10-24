
namespace Common;

using System.Text;
using System.Text.Json;
using Azure.Storage.Queues;
using Model;

public class AzureQueueStorageHelper : IAzureQueueStorageHelper
{
    private async Task<QueueClient> CreateQueueClientAsync(string queueName, string storageAccountName)
    {
        var queueClient = new QueueClient("UseDevelopmentStorage=true", queueName);
        await queueClient.CreateIfNotExistsAsync();
        return queueClient;
    }

    public async Task AddItemsToQueueAsync(string queueName, string storageAccountName, BasicParticipantCsvRecord participantCsvRecord)
    {
        var queueClient = await CreateQueueClientAsync(queueName, storageAccountName);
        if (participantCsvRecord.Participant != null)
        {
            var json = JsonSerializer.Serialize(participantCsvRecord);
            var bytes = Encoding.UTF8.GetBytes(json);
            var receipt = await queueClient.SendMessageAsync(Convert.ToBase64String(bytes));
        }

    }
}