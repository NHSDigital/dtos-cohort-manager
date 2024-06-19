namespace Model;

public class Demographic
{
    public string ResourceId { get; set; }
    public string NhsNumber { get; set; }
    public string Prefix { get; set; }
    public string GivenName { get; set; }
    public string FamilyName { get; set; }
    public string Gender { get; set; }
    public string BirthDate { get; set; }
    public string DeceasedDatetime { get; set; }
    public string GeneralPractitionerCode { get; set; }
    public string ManagingOrganizationCode { get; set; }
    public string CommunicationLanguage { get; set; }
    public string InterpreterRequired { get; set; }
    public string PreferredCommunicationFormat { get; set; }
    public string PreferredContactMethod { get; set; }
    public string PreferredContactTime { get; set; }
    public string BirthPlaceCity { get; set; }
    public string BirthPlaceDistrict { get; set; }
    public string BirthPlaceCountry { get; set; }
    public string RemovalReasonCode { get; set; }
    public string RemovalEffectiveStart { get; set; }
    public string RemovalEffectiveEnd { get; set; }
    public string HomeAddressLine1 { get; set; }
    public string HomeAddressLine2 { get; set; }
    public string HomeAddressLine3 { get; set; }
    public string HomeAddressCity { get; set; }
    public string HomeAddressPostcode { get; set; }
    public string HomePhoneNumber { get; set; }
    public string HomeEmailAddress { get; set; }
    public string HomePhoneTextphone { get; set; }
    public string EmergencyContactPhoneNumber { get; set; }
}
