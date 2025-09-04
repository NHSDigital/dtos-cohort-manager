namespace Common;

public class MeshPollerStub : IMeshPoller
{
    public Task<bool> ExecuteHandshake(string mailboxId)
    {
        return Task.FromResult(true);
    }

    public Task<bool> ShouldExecuteHandshake(string mailboxId, string configFileName)
    {
        return Task.FromResult(false);
    }
}
