
namespace Common;

using System.Text;
using System.Text.Json;
using Azure.Storage.Queues;
using Model;

public class AzureQueueStorageHelper : IAzureQueueStorageHelper
{
    private readonly QueueClient _queueClient;

    public AzureQueueStorageHelper()
    {
        var storageConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
        _queueClient = new QueueClient(storageConnectionString, "add-participant-queue");
    }

    public async Task AddItemToQueueAsync<T>(T participantCsvRecord)
    {
        var json = JsonSerializer.Serialize(participantCsvRecord);
        var bytes = Encoding.UTF8.GetBytes(json);
        await _queueClient.SendMessageAsync(Convert.ToBase64String(bytes));
    }
}
