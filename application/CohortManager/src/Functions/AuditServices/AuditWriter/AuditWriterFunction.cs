namespace NHS.CohortManager.AuditServices;

using System.Text.Json;
using Azure.Storage.Blobs;
using DataServices.Database;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Model;

public class AuditWriterFunction
{
    private readonly DataServicesContext _dbContext;
    private readonly BlobServiceClient _blobService;
    private readonly ILogger<AuditWriterFunction> _logger;
    private const string AuditBlobContainer = "audit-request-snapshots";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public AuditWriterFunction(DataServicesContext dbContext, BlobServiceClient blobService, ILogger<AuditWriterFunction> logger)
    {
        _dbContext = dbContext;
        _blobService = blobService;
        _logger = logger;
    }

    [Function(nameof(AuditWriterFunction))]
    public async Task Run(
        [QueueTrigger("participant-audit-queue", Connection = "AzureWebJobsStorage")] string messageText,
        FunctionContext context)
    {
        ParticipantAuditMessage? audit;
        try
        {
            audit = JsonSerializer.Deserialize<ParticipantAuditMessage>(messageText, JsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialise audit message");
            return;
        }

        if (audit is null)
        {
            _logger.LogError("Failed to deserialise audit message");
            return;
        }

        string? rawDataRef = null;
        if (audit.RequestSnapshot is not null)
        {
            rawDataRef = await WriteSnapshotToBlobAsync(audit);
        }

        var auditLog = new ParticipantAuditLog
        {
            CorrelationId = audit.CorrelationId,
            NhsNumber = audit.NhsNumber,
            BatchId = audit.BatchId,
            CreatedDatetime = audit.CreatedDatetime,
            RecordSource = (int)audit.Source,
            RecordSourceDesc = audit.RecordSourceDesc,
            CreatedBy = audit.CreatedBy,
            ScreeningId = audit.ScreeningId,
            RawDataRef = rawDataRef
        };

        _dbContext.Set<ParticipantAuditLog>().Add(auditLog);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Audit written | NHS: {NhsNumber} | Source: {Source} | Correlation: {CorrelationId}",
            audit.NhsNumber, audit.Source, audit.CorrelationId);
    }

    private async Task<string> WriteSnapshotToBlobAsync(ParticipantAuditMessage audit)
    {
        var container = _blobService.GetBlobContainerClient(AuditBlobContainer);
        await container.CreateIfNotExistsAsync();

        var blobPath = $"{audit.Source}/{audit.CreatedDatetime:yyyy-MM-dd}/{audit.CorrelationId}.json";
        var blobClient = container.GetBlobClient(blobPath);

        var payload = JsonSerializer.SerializeToUtf8Bytes(audit.RequestSnapshot, JsonOptions);
        await blobClient.UploadAsync(new BinaryData(payload), overwrite: true);

        return blobClient.Uri.ToString();
    }
}
