namespace Common.Interfaces;

using Model;

/// <summary>
/// Strategy interface for extracting unextracted cohort distribution participants.
/// Allows different implementations to handle nullable type conversions differently.
/// </summary>
public interface IExtractCohortDistributionRecordsStrategy
{
    /// <summary>
    /// Gets unextracted cohort distribution participants using the strategy's specific logic.
    /// </summary>
    /// <param name="rowCount">Maximum number of participants to extract</param>
    /// <param name="retrieveSupersededRecordsLast">Flag to determine if superseded records should be retrieved last</param>
    /// <returns>List of cohort distribution entities to be extracted</returns>
    Task<List<CohortDistribution>> GetUnextractedParticipants(int rowCount, bool retrieveSupersededRecordsLast);
}
