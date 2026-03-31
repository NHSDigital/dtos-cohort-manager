namespace Common;

using System.Text;
using System.Text.Json;
using Azure.Storage.Queues;
using Common.Interfaces;
using Microsoft.Extensions.Logging;
using Model;

public class AuditQueueSender : IAuditQueueSender
{
    private const string QueueName = "participant-audit-queue";
    private readonly ILogger<AuditQueueSender> _logger;
    private readonly QueueClient _queueClient;

    public AuditQueueSender(ILogger<AuditQueueSender> logger)
    {
        _logger = logger;
        var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
        _queueClient = new QueueClient(connectionString, QueueName);
    }

    public async Task<bool> SendAuditAsync(ParticipantAuditMessage message)
    {
        try
        {
            await _queueClient.CreateIfNotExistsAsync();
            var json = JsonSerializer.Serialize(message);
            var bytes = Encoding.UTF8.GetBytes(json);
            await _queueClient.SendMessageAsync(Convert.ToBase64String(bytes));
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enqueue audit message for QueueName {QueueName}", QueueName);
            return false;
        }
    }
}
