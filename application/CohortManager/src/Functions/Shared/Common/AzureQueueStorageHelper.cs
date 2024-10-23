
namespace Common;

using System.Text.Json;
using Azure.Storage.Queues;
using Model;

public class AzureQueueStorageHelper : IAzureQueueStorageHelper
{
    private async Task<QueueClient> CreateQueueClientAsync(string queueName, string storageAccountName)
    {
        storageAccountName = "UseDevelopmentStorage=true";

        var queueClient = new QueueClient(storageAccountName, queueName);
        await queueClient.CreateIfNotExistsAsync();
        return queueClient;
    }

    public async Task AddItemsToQueueAsync(string queueName, string storageAccountName, List<ParticipantCsvRecord> participantCsvRecords)
    {
        var queueClient = await CreateQueueClientAsync(queueName, storageAccountName);


        var itemsOnQueueAlready = new Dictionary<int, ParticipantCsvRecord>();

        await Parallel.ForEachAsync(participantCsvRecords, async (participant, ct) =>
        {
            if (participant.Participant.ParticipantId != null)
            {
                var participantId = int.Parse(participant.Participant.ParticipantId);
                if (!itemsOnQueueAlready.ContainsKey(int.Parse(participant.Participant.ParticipantId)))
                {
                    var receipt = await queueClient.SendMessageAsync(JsonSerializer.Serialize(participant));
                    itemsOnQueueAlready.Add(participantId, participant);
                }
            }

        });
    }
}