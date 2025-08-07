namespace NHS.CohortManager.ParticipantManagementService;

public interface IBlockParticipantHandler
{
    Task<BlockParticipantResult> BlockParticipant(BlockParticipantDto blockParticipantRequest);
    Task<BlockParticipantResult> GetParticipant(BlockParticipantDto blockParticipantRequest);
    Task<BlockParticipantResult> UnblockParticipant(long nhsNumber);
}
