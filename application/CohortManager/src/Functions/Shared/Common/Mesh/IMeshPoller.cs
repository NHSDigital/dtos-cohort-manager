namespace Common;

public interface IMeshPoller
{
    /// <summary>
    /// Executes a mesh handshake to the given mailboxId
    /// This should be executed every 24 hours for every mesh mailbox
    /// </summary>
    /// <param name="mailboxId"></param>
    /// <returns>boolean, True if successful</returns>
    Task<bool> ExecuteHandshake(string mailboxId);
    /// <summary>
    /// Will check against some state object if the handshake has not been executed in the past 23h55 mins
    /// </summary>
    /// <param name="mailboxId"></param>
    /// <param name="configFileName"></param>
    /// <returns>true is handshake should be executed, false if not</returns>
    Task<bool> ShouldExecuteHandshake(string mailboxId, string configFileName);
}
