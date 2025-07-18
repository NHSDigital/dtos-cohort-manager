namespace Model;

public class ServiceNowParticipant
{
    public required string NhsNumber { get; set; }
    public required string FirstName { get; set; }
    public required string FamilyName { get; set; }
    public required string DateOfBirth { get; set; }
    public required string ServiceNowRecordNumber { get; set; }
}
