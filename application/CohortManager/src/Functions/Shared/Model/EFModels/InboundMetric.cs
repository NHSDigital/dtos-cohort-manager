namespace Model;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class InboundMetric
{
    [Key]
    [Column("METRIC_AUDIT_ID", TypeName = "uniqueidentifier")]
    public Guid MetricAuditId { get; set; }
    [Column("PROCESS_NAME")]
    public required string ProcessName { get; set; }
    [Column("RECEIVED_DATETIME", TypeName = "datetime")]
    public DateTime ReceivedDateTime { get; set; }
    [Column("SOURCE")]
    public required string Source { get; set; }
    [Column("RECORD_COUNT")]
    public int RecordCount { get; set; }
}
