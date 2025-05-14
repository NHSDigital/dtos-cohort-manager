namespace Model;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class BsSelectRequestAudit
{
    [Key]
    [Column("REQUEST_ID", TypeName = "uniqueidentifier")]
    public Guid RequestId { get; set; }
    [Column("STATUS_CODE")]
    [MaxLength(3)]
    public string StatusCode { get; set; }
    [Column("CREATED_DATETIME", TypeName = "datetime")]
    public DateTime CreatedDateTime { get; set; }
}
