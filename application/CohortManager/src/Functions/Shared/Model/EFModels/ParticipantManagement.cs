namespace Model;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class ParticipantManagement
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("PARTICIPANT_ID")]
    public Int64 ParticipantId {get;set;}
    [Column("SCREENING_ID")]
    public Int64 ScreeningId {get;set;}
    [Column("NHS_NUMBER")]
    public Int64 NHSNumber {get;set;}
    [MaxLength(10)]
    [Column("RECORD_TYPE")]
    [Required]
    public string RecordType {get;set;}
    [Column("ELIGIBILITY_FLAG")]
    public Int16 EligibilityFlag {get;set;}
    [MaxLength(10)]
    [Column("REASON_FOR_REMOVAL")]
    public string? ReasonForRemoval {get;set;}
    [Column("REASON_FOR_REMOVAL_FROM_DT")]
    public DateTime? ReasonForRemovalDate {get;set;}
    [MaxLength(10)]
    [Column("BUSINESS_RULE_VERSION")]
    public string? BusinessRuleVersion {get;set;}
    [Column("EXCEPTION_FLAG")]
    public Int16 ExceptionFlag {get;set;}
    [Column("RECORD_INSERT_DATETIME")]
    public DateTime? RecordInsertDateTime {get;set;}
    [Column("RECORD_UPDATE_DATETIME")]
    public DateTime? RecordUpdateDateTime {get;set;}

    //TODO add additonal columns to Model When needed

}
