namespace NHS.CohortManager.ParticipantManagementService;

public class BlockParticipantResult
{
    public BlockParticipantResult(bool success, string? responseMessage = null)
    {
        Success = success;
        ResponseMessage = responseMessage;
    }
    public bool Success { get; set; }
    public string? ResponseMessage { get; set; }
}
