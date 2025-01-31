namespace Model;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("PARTICIPANT_DEMOGRAPHIC")]
public class ParticipantDemographic
{
    [Key]
    [Column("PARTICIPANT_ID")]
    public long ParticipantId { get; set; }

    [Column("NHS_NUMBER")]
    public long NhsNumber { get; set; }

    [Column("SUPERSEDED_BY_NHS_NUMBER")]
    public long? SupersededByNhsNumber { get; set; }

    [Column("PRIMARY_CARE_PROVIDER")]
    public string? PrimaryCareProvider { get; set; }

    [Column("PRIMARY_CARE_PROVIDER_FROM_DT")]
    public string? PrimaryCareProviderFromDate { get; set; }

    [Column("CURRENT_POSTING")]
    public string? CurrentPosting { get; set; }

    [Column("CURRENT_POSTING_FROM_DT")]
    public string? CurrentPostingFromDate { get; set; }

    [Column("NAME_PREFIX")]
    public string? NamePrefix { get; set; }

    [Column("GIVEN_NAME")]
    public string? GivenName { get; set; }

    [Column("OTHER_GIVEN_NAME")]
    public string? OtherGivenName { get; set; }

    [Column("FAMILY_NAME")]
    public string? FamilyName { get; set; }

    [Column("PREVIOUS_FAMILY_NAME")]
    public string? PreviousFamilyName { get; set; }

    [Column("DATE_OF_BIRTH")]
    public string? DateOfBirth { get; set; }

    [Column("GENDER")]
    public short? Gender { get; set; }

    [Column("ADDRESS_LINE_1")]
    public string? AddressLine1 { get; set; }

    [Column("ADDRESS_LINE_2")]
    public string? AddressLine2 { get; set; }

    [Column("ADDRESS_LINE_3")]
    public string? AddressLine3 { get; set; }

    [Column("ADDRESS_LINE_4")]
    public string? AddressLine4 { get; set; }

    [Column("ADDRESS_LINE_5")]
    public string? AddressLine5 { get; set; }

    [Column("POST_CODE")]
    public string? PostCode { get; set; }

    [Column("PAF_KEY")]
    public string? PafKey { get; set; }

    [Column("USUAL_ADDRESS_FROM_DT")]
    public string? UsualAddressFromDate { get; set; }

    [Column("DATE_OF_DEATH")]
    public string? DateOfDeath { get; set; }

    [Column("DEATH_STATUS")]
    public short? DeathStatus { get; set; }

    [Column("TELEPHONE_NUMBER_HOME")]
    public string? TelephoneNumberHome { get; set; }

    [Column("TELEPHONE_NUMBER_HOME_FROM_DT")]
    public string? TelephoneNumberHomeFromDate { get; set; }

    [Column("TELEPHONE_NUMBER_MOB")]
    public string? TelephoneNumberMob { get; set; }

    [Column("TELEPHONE_NUMBER_MOB_FROM_DT")]
    public string? TelephoneNumberMobFromDate { get; set; }

    [Column("EMAIL_ADDRESS_HOME")]
    public string? EmailAddressHome { get; set; }

    [Column("EMAIL_ADDRESS_HOME_FROM_DT")]
    public string? EmailAddressHomeFromDate { get; set; }

    [Column("PREFERRED_LANGUAGE")]
    public string? PreferredLanguage { get; set; }

    [Column("INTERPRETER_REQUIRED")]
    public short? InterpreterRequired { get; set; }

    [Column("INVALID_FLAG")]
    public short? InvalidFlag { get; set; }

    [Column("RECORD_INSERT_DATETIME")]
    public DateTime? RecordInsertDateTime { get; set; }

    [Column("RECORD_UPDATE_DATETIME")]
    public DateTime? RecordUpdateDateTime { get; set; }
}
