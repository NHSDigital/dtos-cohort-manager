
namespace Common;

using System.Text;
using System.Text.Json;
using Azure.Storage.Queues;
using Microsoft.Extensions.Logging;
using Model;

public class AzureQueueStorageHelper : IAzureQueueStorageHelper
{

    private readonly string storageConnectionString;

    public readonly ILogger<AzureQueueStorageHelper> _logger;

    public AzureQueueStorageHelper(ILogger<AzureQueueStorageHelper> logger)
    {
        storageConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
        _logger = logger;
    }

    public async Task<bool> AddItemToQueueAsync<T>(T participantCsvRecord, string queueName)
    {
        var _queueClient = new QueueClient(storageConnectionString, queueName);
        await _queueClient.CreateIfNotExistsAsync();
        var json = JsonSerializer.Serialize(participantCsvRecord);
        var bytes = Encoding.UTF8.GetBytes(json);
        try
        {
            await _queueClient.SendMessageAsync(Convert.ToBase64String(bytes));
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "There was an error while putting item on queue for queue: {queueName}", queueName);
            return false;
        }
    }

    public async Task<List<T>> GetItemsFromQueue<T>(int numberOfItems, string queueName) where T : class
    {
        var _queueClient = new QueueClient(storageConnectionString, queueName);
        var messages = await _queueClient.ReceiveMessagesAsync(maxMessages: numberOfItems);
        List<T> messageList = new List<T>();
        foreach(var message in messages.Value)
        {

            var messageJson =Convert.FromBase64String(message.Body.ToString());
            var messageBody = JsonSerializer.Deserialize<T>(messageJson);
            messageList.Add( messageBody);
            await _queueClient.DeleteMessageAsync(message.MessageId,message.PopReceipt);
        }
        return messageList;
    }
}
