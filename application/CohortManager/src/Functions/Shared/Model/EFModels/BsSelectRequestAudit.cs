namespace Model;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class BsSelectRequestAudit
{
    [Key]
    [Column("REQUEST_ID")]
    public Guid RequestId {get;set;}
    [Column("STATUS_CODE")]
    [MaxLength(3)]
    public string StatusCode {get;set;}
    [Column("CREATED_DATETIME")]
    public DateTime CreatedDateTime {get;set;}
}
