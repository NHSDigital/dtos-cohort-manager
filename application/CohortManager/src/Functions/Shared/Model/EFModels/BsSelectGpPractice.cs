namespace Model;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class BsSelectGpPractice
{
    [Key]
    [Column("GP_PRACTICE_CODE")]
    public string GpPracticeCode { get; set; }
    [Column("BSO")]
    public string BsoCode { get; set; }
    [Column("COUNTRY_CATEGORY")]
    public string CountryCategory { get; set; }
    [Column("AUDIT_ID")]
    public decimal AuditId { get; set; }
    [Column("AUDIT_CREATED_TIMESTAMP")]
    public DateTime AuditCreatedTimeStamp { get; set; }
    [Column("AUDIT_LAST_MODIFIED_TIMESTAMP")]
    public DateTime AuditLastUpdatedTimeStamp { get; set; }
    [Column("AUDIT_TEXT")]
    public string AuditText {get;set;}

}
