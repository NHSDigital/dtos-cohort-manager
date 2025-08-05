namespace NHS.CohortManager.ParticipantManagementService;

public interface IBlockParticipantHandler
{
    Task<BlockParticipantResult> BlockParticipant(BlockParticipantDTO blockParticipantRequest);
    Task<BlockParticipantResult> GetParticipant(BlockParticipantDTO blockParticipantRequest);
}
