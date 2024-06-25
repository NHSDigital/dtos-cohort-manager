namespace Model;

public class BasicParticipantData
{
    public string? RecordType { get; set; }
    public string? ChangeTimeStamp { get; set; }
    public string? SerialChangeNumber { get; set; }
    public string? NhsNumber { get; set; }
    public string? SupersededByNhsNumber { get; set; }
    public string? PrimaryCareProviderEffectiveFrom { get; set; }
    public string? CurrentPostingEffectiveFrom { get; set; }
    public string? PreviousPosting { get; set; }
    public string? PreviousPostingEffectiveFrom { get; set; }
    public string? OtherGivenNames { get; set; }
    public string? PreviousSurname { get; set; }
    public string? AddressLine5 { get; set; }
    public string? PafKey { get; set; }
    public string? UsualAddressEffectiveFromDate { get; set; }
    public string? TelephoneNumberEffectiveFromDate { get; set; }
    public string? MobileNumber { get; set; }
    public string? MobileNumberEffectiveFromDate { get; set; }
    public string? EmailAddressEffectiveFromDate { get; set; }
    public string? InvalidFlag { get; set; }
    public string? ChangeReasonCode { get; set; }
    public string? RemovalReason { get; set; }
    public string? RemovalEffectiveFromDate { get; set; }
}
