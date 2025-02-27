namespace Model;

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class GPPractice
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("GP_PRACTICE_ID")]
    public int GPPracticeId { get; set; }

    [Required]
    [MaxLength(8)]
    [Column("GP_PRACTICE_CODE")]
    public string GPPracticeCode { get; set; }

    [Required]
    [Column("BSO_ORGANISATION_ID")]
    public int BSOOrganisationId { get; set; }

    [MaxLength(4)]
    [Column("OUTCODE")]
    public string? Outcode { get; set; }

    [Column("GP_PRACTICE_GROUP_ID")]
    public int? GPPracticeGroupId { get; set; }

    [Required]
    [Column("TRANSACTION_ID")]
    public int TransactionId { get; set; }

    [Required]
    [Column("TRANSACTION_APP_DATE_TIME")]
    public DateTimeOffset TransactionAppDateTime { get; set; }

    [Required]
    [Column("TRANSACTION_USER_ORG_ROLE_ID")]
    public int TransactionUserOrgRoleId { get; set; }

    [Required]
    [Column("TRANSACTION_DB_DATE_TIME")]
    public DateTimeOffset TransactionDbDateTime { get; set; }

    [MaxLength(100)]
    [Column("GP_PRACTICE_NAME")]
    public string? GPPracticeName { get; set; }

    [MaxLength(35)]
    [Column("ADDRESS_LINE_1")]
    public string? AddressLine1 { get; set; }

    [MaxLength(35)]
    [Column("ADDRESS_LINE_2")]
    public string? AddressLine2 { get; set; }

    [MaxLength(35)]
    [Column("ADDRESS_LINE_3")]
    public string? AddressLine3 { get; set; }

    [MaxLength(35)]
    [Column("ADDRESS_LINE_4")]
    public string? AddressLine4 { get; set; }

    [MaxLength(35)]
    [Column("ADDRESS_LINE_5")]
    public string? AddressLine5 { get; set; }

    [MaxLength(8)]
    [Column("POSTCODE")]
    public string? Postcode { get; set; }

    [MaxLength(12)]
    [Column("TELEPHONE_NUMBER")]
    public string? TelephoneNumber { get; set; }

    [Column("OPEN_DATE")]
    public DateTime? OpenDate { get; set; }

    [Column("CLOSE_DATE")]
    public DateTime? CloseDate { get; set; }

    [Column("FAILSAFE_DATE")]
    public DateTime? FailsafeDate { get; set; }

    [Required]
    [MaxLength(1)]
    [Column("STATUS_CODE")]
    public string StatusCode { get; set; }

    [Required]
    [Column("LAST_UPDATED_DATE_TIME")]
    public DateTimeOffset LastUpdatedDateTime { get; set; }

    [Required]
    [Column("ACTIONED")]
    public bool Actioned { get; set; } = false;

    [Column("LAST_ACTIONED_BY_USER_ORG_ROLE_ID")]
    public int? LastActionedByUserOrgRoleId { get; set; }

    [Column("LAST_ACTIONED_ON")]
    public DateTimeOffset? LastActionedOn { get; set; }
}
