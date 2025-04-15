namespace NHS.CohortManager.CohortDistribution;

public class DeleteParticipantRequestBody
{
    public string? NhsNumber { get; set; }
    public string? FamilyName { get; set; }
    public DateTime? DateOfBirth { get; set; }
}
