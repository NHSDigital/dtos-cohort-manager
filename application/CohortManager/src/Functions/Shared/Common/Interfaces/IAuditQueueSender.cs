namespace Common.Interfaces;

using Model;

public interface IAuditQueueSender
{
    Task<bool> SendAuditAsync(ParticipantAuditMessage message);
}
