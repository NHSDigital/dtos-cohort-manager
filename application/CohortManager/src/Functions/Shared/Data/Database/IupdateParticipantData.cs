using System.Data;
using Microsoft.Identity.Client;
using Model;

namespace Data.Database;

public interface IUpdateParticipantData
{
    public bool UpdateParticipantAsEligible(Participant participant, char isActive);
    public Task<bool> UpdateParticipantDetails(Participant participantData);
    public Participant GetParticipant(string NHSId);
}
