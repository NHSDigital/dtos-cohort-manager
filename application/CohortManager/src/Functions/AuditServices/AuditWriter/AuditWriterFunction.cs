namespace NHS.CohortManager.AuditServices;

using System.Text.Json;
using DataServices.Database;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Model;

public class AuditWriterFunction
{
    private readonly DataServicesContext _dbContext;
    private readonly ILogger<AuditWriterFunction> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public AuditWriterFunction(DataServicesContext dbContext, ILogger<AuditWriterFunction> logger)
    {
        _dbContext = dbContext;
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
            RawDataRef = audit.RawDataRef
        };

        _dbContext.Set<ParticipantAuditLog>().Add(auditLog);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Audit written | NHS: {NhsNumber} | Source: {Source} | Correlation: {CorrelationId}",
            audit.NhsNumber, audit.Source, audit.CorrelationId);
    }
}
