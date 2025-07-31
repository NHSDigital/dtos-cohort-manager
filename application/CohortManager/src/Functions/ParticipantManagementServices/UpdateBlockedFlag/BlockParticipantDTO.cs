namespace NHS.CohortManager.ParticipantManagementService;

public class BlockParticipantDTO
{
    public required long NhsNumber { get; set; }
    public required DateOnly DateOfBirth { get; set; }
    public required string FamilyName { get; set; }
}
