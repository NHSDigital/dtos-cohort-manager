namespace Model;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class ServicenowCase
{
    [Key]
    [Column("ID")]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(10)]
    [Column("SERVICENOW_ID")]
    public required string ServicenowId { get; set; }

    [Column("NHS_NUMBER")]
    public Int64? NhsNumber { get; set; }

    [MaxLength(10)]
    [Column("STATUS")]
    public string? Status { get; set; }

    [Column("RECORD_INSERT_DATETIME", TypeName = "datetime")]
    public DateTime? RecordInsertDatetime { get; set; }

    [Column("RECORD_UPDATE_DATETIME", TypeName = "datetime")]
    public DateTime? RecordUpdateDatetime { get; set; }
}
