namespace NHS.Screening.DemographicDurableFunction;

using System.Text;
using System.Text.Json;
using Common;

public class DurableAddHelper
{
    private readonly IQueueClientFactory _queueClientFactory;

    public DurableAddHelper(IQueueClientFactory queueClientFactory)
    {
        _queueClientFactory = queueClientFactory;
    }

    public async Task<bool> AddItemToQueueAsync<T>(T participantCsvRecord, string queueName)
    {
        var _queueClient = _queueClientFactory.CreateClient(queueName);
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
            return false;
        }
    }
}