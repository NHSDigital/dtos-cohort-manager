namespace Model;

using System.Text.Json.Serialization;
using Model.Enums;

public class ParticipantAuditMessage
{
    public Guid CorrelationId { get; set; } = Guid.NewGuid();
    public Guid? BatchId { get; set; }
    public required string NhsNumber { get; set; }
    public required AuditSource Source { get; set; }
    public string? RecordSourceDesc { get; set; }
    public required DateTime CreatedDatetime { get; set; }
    public required string CreatedBy { get; set; }
    public int? ScreeningId { get; set; }
    public string? RawDataRef { get; set; }

    [JsonIgnore]
    public object? RequestSnapshot { get; set; }
}
