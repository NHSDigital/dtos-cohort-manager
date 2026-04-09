namespace Common;

using System.Text.Json;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Common.Interfaces;
using Microsoft.Extensions.Logging;
using Model;

public class AuditQueueSender : IAuditQueueSender
{
    private const string QueueName = "participant-audit-queue";
    private const string AuditBlobContainer = "audit-request-snapshots";
    private readonly ILogger<AuditQueueSender> _logger;
    private readonly QueueClient _queueClient;
    private readonly BlobServiceClient _blobServiceClient;
    private readonly Lazy<Task> _queueInitialization;
    private readonly Lazy<Task<BlobContainerClient>> _containerInitialization;

    public AuditQueueSender(ILogger<AuditQueueSender> logger)
    {
        _logger = logger;
        var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("The AzureWebJobsStorage environment variable must be configured to send audit messages.");
        }
        _queueClient = new QueueClient(connectionString, QueueName, new QueueClientOptions
        {
            MessageEncoding = QueueMessageEncoding.Base64
        });
        _blobServiceClient = new BlobServiceClient(connectionString);
        _queueInitialization = new Lazy<Task>(() => _queueClient.CreateIfNotExistsAsync());
        _containerInitialization = new Lazy<Task<BlobContainerClient>>(async () =>
        {
            var container = _blobServiceClient.GetBlobContainerClient(AuditBlobContainer);
            await container.CreateIfNotExistsAsync();
            return container;
        });
    }

    public async Task<bool> SendAuditAsync(ParticipantAuditMessage message)
    {
        try
        {
            if (message.RequestSnapshot is not null)
            {
                message.RawDataRef = await WriteSnapshotToBlobAsync(message);
            }

            await _queueInitialization.Value;
            var json = JsonSerializer.Serialize(message);
            await _queueClient.SendMessageAsync(json);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enqueue audit message for QueueName {QueueName}", QueueName);
            return false;
        }
    }

    private async Task<string> WriteSnapshotToBlobAsync(ParticipantAuditMessage message)
    {
        var container = await _containerInitialization.Value;

        var blobPath = $"{message.Source}/{message.CreatedDatetime:dd-MM-yyyy}/{message.CorrelationId}.json";
        var blobClient = container.GetBlobClient(blobPath);

        var payload = JsonSerializer.SerializeToUtf8Bytes(message.RequestSnapshot);
        await blobClient.UploadAsync(new BinaryData(payload), overwrite: true);

        return blobClient.Uri.ToString();
    }
}
