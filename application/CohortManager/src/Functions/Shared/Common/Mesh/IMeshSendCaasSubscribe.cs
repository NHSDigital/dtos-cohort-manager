namespace Common;

public interface IMeshSendCaasSubscribe
{
    Task<string> SendSubscriptionRequest(long nhsNumber, string toMailbox, string fromMailbox);
}
