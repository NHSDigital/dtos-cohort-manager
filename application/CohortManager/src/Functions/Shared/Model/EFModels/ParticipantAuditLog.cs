namespace Model;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class ParticipantAuditLog
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("AUDIT_ID")]
    public long AuditId { get; set; }

    [Column("CORRELATION_ID")]
    public Guid CorrelationId { get; set; }

    [Column("BATCH_ID")]
    public Guid? BatchId { get; set; }

    [Required]
    [MaxLength(10)]
    [Column("NHS_NUMBER", TypeName = "nvarchar(10)")]
    public required string NhsNumber { get; set; }

    [Column("CREATED_DATETIME", TypeName = "datetime2(7)")]
    public DateTime CreatedDatetime { get; set; }

    [Column("RECORD_SOURCE")]
    public int RecordSource { get; set; }

    [MaxLength(255)]
    [Column("RECORD_SOURCE_DESC", TypeName = "nvarchar(255)")]
    public string? RecordSourceDesc { get; set; }

    [MaxLength(255)]
    [Column("CREATED_BY", TypeName = "nvarchar(255)")]
    public string? CreatedBy { get; set; }

    [Column("SCREENING_ID")]
    public int? ScreeningId { get; set; }

    [MaxLength(2048)]
    [Column("RAW_DATA_REF", TypeName = "nvarchar(2048)")]
    public string? RawDataRef { get; set; }
}
