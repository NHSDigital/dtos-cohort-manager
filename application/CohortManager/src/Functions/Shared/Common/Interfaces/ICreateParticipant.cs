namespace Common;

using Model;

public interface ICreateParticipant
{
    public Participant CreateResponseParticipantModel(BasicParticipantData participant, Demographic demographic);
    public CohortDistributionParticipant CreateCohortDistributionParticipantModel(ParticipantManagement participant, Demographic demographic);
}
