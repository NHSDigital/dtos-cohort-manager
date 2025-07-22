namespace NHS.CohortManager.ParticipantManagementService;
public class DeleteParticipantRequestBody
{
    public string? NhsNumber { get; set; }
    public string? FamilyName { get; set; }
    public DateTime? DateOfBirth { get; set; }
}
