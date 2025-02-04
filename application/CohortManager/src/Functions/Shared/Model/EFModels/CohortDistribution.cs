namespace Model;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class CohortDistribution
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("BS_COHORT_DISTRIBUTION_ID")]
    public Int32 CohortDistributionId {get;set;}
    [Column("PARTICIPANT_ID")]
    public Int64 ParticipantId {get;set;}
    [Column("NHS_NUMBER")]
    public Int64 NHSNumber {get;set;}
    [Column("SUPERSEDED_NHS_NUMBER")]
    public Int64 SupersededNHSNumber {get;set;}
    [MaxLength(10)]
    [Column("PRIMARY_CARE_PROVIDER")]
    [Required]
    public string PrimaryCareProvider {get;set;}
    [Column("PRIMARY_CARE_PROVIDER_FROM_DT")]
    public DateTime? PrimaryCareProviderDate {get;set;}
    [MaxLength(35)]
    [Column("NAME_PREFIX")]
    public string? NamePrefix {get;set;}
    [MaxLength(100)]
    [Column("GIVEN_NAME")]
    public string? GivenName {get;set;}
    [MaxLength(100)]
    [Column("OTHER_GIVEN_NAME")]
    public string? OtherGivenName {get;set;}
    [MaxLength(100)]
    [Column("FAMILY_NAME")]
    public string? FamilyName {get;set;}
    [MaxLength(100)]
    [Column("PREVIOUS_FAMILY_NAME")]
    public string? PreviousFamilyName {get;set;}
    [Column("DATE_OF_BIRTH")]
    public DateTime? DateOfBirth {get;set;}
    [Column("GENDER")]
    public Int16 Gender {get;set;}
    [MaxLength(100)]
    [Column("ADDRESS_LINE_1")]
    public string? AddressLine1 {get;set;}
    [MaxLength(100)]
    [Column("ADDRESS_LINE_2")]
    public string? AddressLine2 {get;set;}
    [MaxLength(100)]
    [Column("ADDRESS_LINE_3")]
    public string? AddressLine3 {get;set;}
    [MaxLength(100)]
    [Column("ADDRESS_LINE_4")]
    public string? AddressLine4 {get;set;}
    [MaxLength(100)]
    [Column("ADDRESS_LINE_5")]
    public string? AddressLine5 {get;set;}
    [MaxLength(10)]
    [Column("POST_CODE")]
    public string? PostCode {get;set;}
    [Column("USUAL_ADDRESS_FROM_DT")]
    public DateTime? UsualAddressFromDt {get;set;}
    [MaxLength(10)]
    [Column("CURRENT_POSTING")]
    public string? CurrentPosting {get;set;}
    [Column("CURRENT_POSTING_FROM_DT")]
    public DateTime? CurrentPostingFromDt {get;set;}
    [Column("DATE_OF_DEATH")]
    public DateTime? DateOfDeath {get;set;}
    [MaxLength(35)]
    [Column("TELEPHONE_NUMBER_HOME")]
    public string? TelephoneNumberHome {get;set;}
    [Column("TELEPHONE_NUMBER_HOME_FROM_DT")]
    public DateTime? TelephoneNumberHomeFromDt {get;set;}
    [MaxLength(35)]
    [Column("TELEPHONE_NUMBER_MOB")]
    public string? TelephoneNumberMob {get;set;}
    [Column("TELEPHONE_NUMBER_MOB_FROM_DT")]
    public DateTime? TelephoneNumberMobFromDt {get;set;}
    [MaxLength(100)]
    [Column("EMAIL_ADDRESS_HOME")]
    public string? EmailAddressHome {get;set;}
    [Column("EMAIL_ADDRESS_HOME_FROM_DT")]
    public DateTime? EmailAddressHomeFromDt {get;set;}
    [MaxLength(35)]
    [Column("PREFERRED_LANGUAGE")]
    public string? PreferredLanguage {get;set;}
    [Column("INTERPRETER_REQUIRED")]
    public Int16 InterpreterRequired {get;set;}
    [MaxLength(10)]
    [Column("REASON_FOR_REMOVAL")]
    public string? ReasonForRemoval {get;set;}
    [Column("REASON_FOR_REMOVAL_FROM_DT")]
    public DateTime? ReasonForRemovalDate {get;set;}
    [Column("IS_EXTRACTED")]
    public Int16 IsExtracted {get;set;}
    [Column("RECORD_INSERT_DATETIME")]
    public DateTime? RecordInsertDateTime {get;set;}
    [Column("RECORD_UPDATE_DATETIME")]
    public DateTime? RecordUpdateDateTime {get;set;}
    [Column("REQUEST_ID")]
    public Guid RequestId { get; set; }


}
