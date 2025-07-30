namespace Model;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class ParticipantManagement
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("PARTICIPANT_ID")]
    public Int64 ParticipantId { get; set; }
    [Column("SCREENING_ID")]
    public Int64 ScreeningId { get; set; }
    [Column("NHS_NUMBER")]
    public Int64 NHSNumber { get; set; }
    [MaxLength(10)]
    [Column("RECORD_TYPE")]
    [Required]
    public string RecordType { get; set; }
    [Column("ELIGIBILITY_FLAG")]
    public Int16 EligibilityFlag { get; set; }
    [MaxLength(10)]
    [Column("REASON_FOR_REMOVAL")]
    public string? ReasonForRemoval { get; set; }
    [Column("REASON_FOR_REMOVAL_FROM_DT", TypeName = "datetime")]
    public DateTime? ReasonForRemovalDate { get; set; }
    [MaxLength(10)]
    [Column("BUSINESS_RULE_VERSION")]
    public string? BusinessRuleVersion { get; set; }
    [Column("EXCEPTION_FLAG")]
    public Int16 ExceptionFlag { get; set; }
    [Column("RECORD_INSERT_DATETIME", TypeName = "datetime")]
    public DateTime? RecordInsertDateTime { get; set; }
    [Column("BLOCKED_FLAG")]
    public Int16 BlockedFlag { get; set; }
    [Column("REFERRAL_FLAG")]
    public Int16 ReferralFlag { get; set; }
    [Column("RECORD_UPDATE_DATETIME", TypeName = "datetime")]
    public DateTime? RecordUpdateDateTime { get; set; }
    [Column("NEXT_TEST_DUE_DATE", TypeName = "datetime")]
    public DateTime? NextTestDueDate { get; set; }
    [Column("NEXT_TEST_DUE_DATE_CALC_METHOD")]
    public string? NextTestDueDateCalcMethod { get; set; }
    [Column("PARTICIPANT_SCREENING_STATUS")]
    public string? ParticipantScreeningStatus { get; set; }
    [Column("SCREENING_CEASED_REASON")]
    public string? ScreeningCeasedReason { get; set; }
    [Column("IS_HIGHER_RISK")]
    public Int16? IsHigherRisk { get; set; }
    [Column("IS_HIGHER_RISK_ACTIVE")]
    public Int16? IsHigherRiskActive { get; set; }
    [Column("HIGHER_RISK_NEXT_TEST_DUE_DATE", TypeName = "datetime")]
    public DateTime? HigherRiskNextTestDueDate { get; set; }
    [Column("HIGHER_RISK_REFERRAL_REASON_ID")]
    public int? HigherRiskReferralReasonId { get; set; }
    [Column("DATE_IRRADIATED", TypeName = "datetime")]
    public DateTime? DateIrradiated { get; set; }
    [Column("GENE_CODE_ID")]
    public int? GeneCodeId { get; set; }
    [Column("SRC_SYSTEM_PROCESSED_DATETIME", TypeName = "datetime")]
    public DateTime? SrcSysProcessedDateTime { get; set; }
}
