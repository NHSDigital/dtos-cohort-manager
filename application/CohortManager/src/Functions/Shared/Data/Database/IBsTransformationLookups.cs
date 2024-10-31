namespace Data.Database;

using Model;

public interface IBsTransformationLookups {
    public string GetGivenName(string participantId);
    public string GetFamilyName(string participantId);
    public string GetName(string participantId, string nameType);
    public CohortDistributionParticipant GetAddress(CohortDistributionParticipant participant);
}
