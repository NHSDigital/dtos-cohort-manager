namespace Data.Database;

using Model;

public interface IBsTransformationLookups
{
    public string GetGivenName(string participantId);
    public string GetFamilyName(string participantId);
    public CohortDistributionParticipant GetAddress(CohortDistributionParticipant participant);
    public bool ParticipantIsInvalid(string participantId);
    public string GetPrimaryCareProvider(string nhsNumber);
}
