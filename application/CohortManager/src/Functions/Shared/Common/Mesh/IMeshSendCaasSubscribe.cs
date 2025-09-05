namespace Common;

/// <summary>
/// Contract for sending CAAS subscription requests via MESH.
/// </summary>
public interface IMeshSendCaasSubscribe
{
    /// <summary>
    /// Sends a CAAS subscription request for the given NHS number.
    /// </summary>
    /// <param name="nhsNumber">The patient NHS number.</param>
    /// <param name="toMailbox">Destination MESH mailbox ID.</param>
    /// <param name="fromMailbox">Source MESH mailbox ID.</param>
    /// <returns>The MESH message ID on success; otherwise null.</returns>
    Task<string?> SendSubscriptionRequest(long nhsNumber, string toMailbox, string fromMailbox);
}
