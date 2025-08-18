namespace Model;

using Model.Enums;

/// <summary>
/// Interface representing participant demographic data
/// </summary>
public interface IDemographic
{
    /// <summary>
    /// The participant's NHS number
    /// </summary>
    public string NhsNumber { get; set; }
    /// <summary>
    /// The NHS number that has superseded the NHS number above.
    /// </summary>
    public string? SupersededByNhsNumber { get; set; }
    /// <summary>
    /// The code of the primary care provider (GP practice) that the
    /// participant is registered with
    /// </summary>
    /// <remarks> AKA "GP practice code" </remarks>
    public string? PrimaryCareProvider { get; set; }
    /// <summary>
    /// The date from when a participant is associated with
    /// a particular primary care provider
    /// </summary>
    public string? PrimaryCareProviderEffectiveFromDate { get; set; }
    /// <summary>
    /// A code representing the country of origin for a participant
    /// </summary>
    /// <remarks>
    /// This can either be an NHAIS Cipher, or a country or institution 
    /// code (for defense medical services and health & justice)
    /// </remarks>
    public string? CurrentPosting { get; set; }
    /// <summary>
    /// The date from when the current posting is effective for a participant
    /// </summary>
    public string? CurrentPostingEffectiveFromDate { get; set; }
    /// <summary>
    /// The participant's name prefix (title)
    /// </summary>
    public string? NamePrefix { get; set; }
    /// <summary>
    /// The participant's first name
    /// </summary>
    /// <remarks> AKA Given Name </remarks>
    public string? FirstName { get; set; }
    /// <summary>
    /// Additional first name(s)/ middle name(s)
    /// </summary>
    public string? OtherGivenNames { get; set; }
    /// <summary>
    /// The participant's surname/ last name
    /// </summary>
    public string? FamilyName { get; set; }
    /// <summary>
    /// Most recent historic version of Family Name that is different to Family Name
    /// </summary>
    public string? PreviousFamilyName { get; set; }
    /// <summary>
    /// The participant's date of birth
    /// </summary>
    public string? DateOfBirth { get; set; }
    /// <summary>
    /// The participant's gender <see cref="Enums.Gender"/>
    /// </summary>
    public Gender? Gender { get; set; }
    /// <summary>
    /// The first line of the participant's address
    /// </summary>
    public string? AddressLine1 { get; set; }
    /// <summary>
    /// The second line of the participant's address
    /// </summary>
    public string? AddressLine2 { get; set; }
    /// <summary>
    /// The third line of the participant's address
    /// </summary>
    public string? AddressLine3 { get; set; }
    /// <summary>
    /// The fourth line of the participant's address
    /// </summary>
    public string? AddressLine4 { get; set; }
    /// <summary>
    /// The fifth line of the participant's address
    /// </summary>
    public string? AddressLine5 { get; set; }
    /// <summary>
    /// The code assigned by Royal Mail to identify postal delivery areas
    /// for a Participant 
    /// </summary>
    public string? Postcode { get; set; }
    /// <summary>
    /// the unique Royal Mail Postcode Address File Directory key for the
    /// address of the participant
    /// </summary>
    public string? PafKey { get; set; }
    /// <summary>
    /// The date from when the current address is effective for a participant
    /// </summary>
    public string? UsualAddressEffectiveFromDate { get; set; }
    /// <summary>
    /// The Participant's death date as registered in NHS system
    /// </summary>
    public string? DateOfDeath { get; set; }
    /// <summary>
    /// The participant's death <see cref="Status"/>
    /// </summary>
    public Status? DeathStatus { get; set; }
    /// <summary>
    /// Home telephone number for the participant
    /// </summary>
    public string? TelephoneNumber { get; set; }
    /// <summary>
    /// The date from when the current home telephone number is
    /// effective for a participant
    /// </summary>
    public string? TelephoneNumberEffectiveFromDate { get; set; }
    /// <summary>
    /// Mobile telephone number for the participant
    /// </summary>
    public string? MobileNumber { get; set; }
    /// <summary>
    /// The date from when the current mobile telephone number is
    /// effective for a participant
    /// </summary>
    public string? MobileNumberEffectiveFromDate { get; set; }
    /// <summary>
    /// The participant's email address
    /// </summary>
    public string? EmailAddress { get; set; }
    /// <summary>
    /// The date from when the email address is
    /// effective for a participant
    /// </summary>
    public string? EmailAddressEffectiveFromDate { get; set; }
    /// <summary>
    /// The language code (per ISO639-1) of the language the participant
    /// would prefer to use to communicate with their health care provider
    /// </summary>
    public string? PreferredLanguage { get; set; }
    /// <summary>
    /// Bool flag (0/ 1) that indicates if the participant requires
    /// an interpreter for communication during the course of screening
    /// </summary>
    public string? IsInterpreterRequired { get; set; }
    /// <summary>
    /// Whether or not the participant is invalid
    /// </summary>
    public string? InvalidFlag { get; set; }
}