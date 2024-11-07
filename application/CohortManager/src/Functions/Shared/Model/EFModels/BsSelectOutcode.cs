namespace Model;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
public class BsSelectOutCode
{
    [Key]
    [Column("OUTCODE")]
    public string Outcode {get;set;}
    [Column("BSO")]
    public string BSO {get;set;}
    [Column("AUDIT_ID")]
    public decimal AuditId {get;set;}
    [Column("AUDIT_CREATED_TIMESTAMP")]
    public DateTime AuditCreatedTimeStamp {get;set;}
    [Column("AUDIT_LAST_MODIFIED_TIMESTAMP")]
    public DateTime AuditLastModifiedTimeStamp {get;set;}
    [Column("AUDIT_TEXT")]
    public string AuditText {get;set;}

}
