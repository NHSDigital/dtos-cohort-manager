namespace Model;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ParquetSharp;

public class BsoOrganisation
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("BSO_ORGANISATION_ID")]
    public int BsoOrganisationId {get;set;}
    [MaxLength(4)]
    [Column("BSO_ORGANISATION_CODE")]
    public string BsoOrganisationCode {get;set;}
    [MaxLength(60)]
    [Column("BSO_ORGANISATION_NAME")]
    public string BsoOrganisationName {get;set;}
    [Column("SAFETY_PERIOD")]
    public byte SafetyPeriod {get;set;}
    [Column("RISP_RECALL_INTERVAL")]
    public byte RispRecallInterval {get;set;}
    [Column("TRANSACTION_ID")]
    public int? TransactionId {get;set;}
    [Column("TRANSACTION_APP_DATE_TIME")]
    public DateTime TransactionAppDateTime {get;set;}
    [Column("TRANSACTION_USER_ORG_ROLE_ID")]
    public int? TransactionUserOrgRoleId {get;set;}
    [Column("TRANSACTION_DB_DATE_TIME")]
    public DateTime TransactionDbDateTime {get;set;}
    [Column("IGNORE_SELF_REFERRALS")]
    public bool IgnoreSelfReferrals {get;set;}
    [Column("IGNORE_GP_REFERRALS")]
    public bool IgnoreGPReferrals {get;set;}
    [Column("IGNORE_EARLY_RECALL")]
    public bool IgnoreEarlyRecall {get;set;}
    [Column("IS_ACTIVE")]
    public bool IsActive {get;set;}
    [Column("LOWER_AGE_RANGE")]
    public byte LowerAgeRange {get;set;}
    [Column("UPPER_AGE_RANGE")]
    public byte UpperAgeRange {get;set;}
    [Column("LINK_CODE")]
    public string LinkCode {get;set;}
    [Column("FOA_MAX_OFFSET")]
    public byte FoaMaxOffset {get;set;}
    [Column("BSO_RECALL_INTERVAL")]
    public byte BsoRecallInterval {get;set;}
    [Column("ADDRESS_LINE_1")]
    [MaxLength(35)]
    public string? AddressLine2 {get;set;}
    [Column("ADDRESS_LINE_3")]
    [MaxLength(35)]
    public string? AddressLine3 {get;set;}
    [Column("ADDRESS_LINE_4")]
    [MaxLength(35)]
    public string? AddressLine4 {get;set;}
    [Column("ADDRESS_LINE_5")]
    [MaxLength(35)]
    public string? AddressLine5 {get;set;}
    [Column("POSTCODE")]
    [MaxLength(8)]
    public string? PostCode {get;set;}
    [Column("TELEPHONE_NUMBER")]
    [MaxLength(18)]
    public string? TelephoneNumber {get;set;}
    [Column("EXTENSION")]
    [MaxLength(35)]
    public string? Extension {get;set;}
    [Column("FAX_NUMBER")]
    [MaxLength(35)]
    public string? FaxNumber {get;set;}
    [Column("EMAIL_ADDRESS")]
    [MaxLength(35)]
    public string? EmailAddress {get;set;}
    [Column("OUTGOING_TRANSFER_NUMBER")]
    public int OutgoingTransferNumber {get;set;}
    [Column("INVITE_LIST_SEQUENCE_NUMBER")]
    public int InviteListSequenceNumber {get;set;}
    [Column("FAILSAFE_DATE_OF_MONTH")]
    public byte FailSafeDateOfMonth {get;set;}
    [Column("FAILSAFE_MONTHS")]
    public byte FailSafeMonths {get;set;}
    [Column("FAILSAFE_MIN_AGE_YEARS")]
    public byte FailSafeMinAgeYears {get;set;}
    [Column("FAILSAFE_MIN_AGE_MONTHS")]
    public byte FailSafeMinAgeMonths {get;set;}
    [Column("FAILSAFE_MAX_AGE_YEARS")]
    public byte FailSafeMaxAgeYears {get;set;}
    [Column("FAILSAFE_MAX_AGE_MONTHS")]
    public byte FailSafeMaxAgeMonths {get;set;}
    [Column("FAILSAFE_LAST_RUN")]
    public DateTime FailSafeLastRun {get;set;}
    [Column("IS_AGEX")]
    public bool IsAgex {get;set;}
    [Column("IS_AGEX_ACTIVE")]
    public bool IsAgexActive {get;set;}
    [Column("AUTO_BATCH_LAST_RUN")]
    public DateTime? AutoBatchLastRun {get;set;}
    [Column("AUTO_BATCH_MAX_DATE_TIME_PROCESSED")]
    public DateTime? AutoBatchMaxDateTimeProcessed {get;set;}
    [Column("BSO_REGION_ID")]
    public int? BsoRegionId {get;set;}
    [Column("ADMIN_EMAIL_ADDRESS")]
    [MaxLength(100)]
    public string? AdminEmailAddress {get;set;}
    [Column("IEP_DETAILS")]
    public string? IepDetails {get;set;}
    [Column("NOTES")]
    public string? Notes {get;set;}
    [Column("RLP_DATE_ENABLED")]
    public DateTime? RlpDateEnabled {get;set;}


}
