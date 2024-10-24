
namespace Common;

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

    public async Task AddItemsToQueueAsync(string queueName, string storageAccountName, List<BasicParticipantCsvRecord> participantCsvRecords)
    {
        var queueClient = await CreateQueueClientAsync(queueName, storageAccountName);


        var itemsOnQueueAlready = new Dictionary<Guid, BasicParticipantCsvRecord>();

        await Parallel.ForEachAsync(participantCsvRecords, async (participant, ct) =>
        {
            if (participant.Participant != null)
            {
                if (!itemsOnQueueAlready.ContainsKey(participant.Participant.ParticipantUUID))
                {
                    var receipt = await queueClient.SendMessageAsync(JsonSerializer.Serialize(participant));
                    itemsOnQueueAlready.Add(participant.Participant.ParticipantUUID, participant);
                }
            }

        });
    }
}