using Model;

namespace Common;

public interface ICreateParticipant
{
    public Participant CreateResponseParticipantModel(Participant participant, Demographic demographic);
}