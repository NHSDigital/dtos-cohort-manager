namespace Common;

using System.Threading.Tasks;

public class MeshSendCaasSubscribeStub : IMeshSendCaasSubscribe
{
    public async Task<string> SendSubscriptionRequest(long nhsNumber, string toMailbox, string fromMailbox)
    {
        await Task.CompletedTask;
        return $"STUB_{Guid.NewGuid():N}";
    }
}
