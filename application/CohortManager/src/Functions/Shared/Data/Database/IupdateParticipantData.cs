using System.Data;
using Microsoft.Identity.Client;
using Model;

namespace Data.Database;

public interface IUpdateParticipantData
{
    public bool UpdateParticipantAsEligible(Participant participant, char isActive);
    public bool UpdateParticipantDetails(Participant participantData);
    public int GetParticipantId(string NHSId);
}
