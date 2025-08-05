namespace Model;

public class ServiceNowParticipant
{
    public required string ServiceNowCaseNumber { get; set; }
    public required long ScreeningId { get; set; }
    public required long NhsNumber { get; set; }
    public required string FirstName { get; set; }
    public required string FamilyName { get; set; }
    public required DateOnly DateOfBirth { get; set; }
    public required string BsoCode { get; set; }
    public required string ReasonForAdding { get; set; }
    public string? RequiredGpCode { get; set; }
}
