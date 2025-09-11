namespace Model.DTO;

public class ErrorRecordDto
{
    public string? NhsNumber { get; set; }
    public string? SupersededByNhsNumber { get; set; }
    public string? FirstName { get; set; }
    public string? FamilyName { get; set; }
    public string? DateOfBirth { get; set; }
    public short? Gender { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? AddressLine3 { get; set; }
    public string? AddressLine4 { get; set; }
    public string? AddressLine5 { get; set; }
    public string? Postcode { get; set; }
    public string? PrimaryCareProvider { get; set; }
    public string? TelephoneNumber { get; set; }
    public string? EmailAddress { get; set; }
}
