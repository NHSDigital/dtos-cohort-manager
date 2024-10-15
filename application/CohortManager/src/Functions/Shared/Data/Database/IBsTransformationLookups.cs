namespace Data.Database;

using Model;

public interface IBsTransformationLookups
{
    public string GetName(string participantId, string nameType);
    public CohortDistributionParticipant GetAddress(CohortDistributionParticipant participant);
}