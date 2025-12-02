namespace Data.Database;

using Common.Interfaces;
using DataServices.Client;
using Model;

/// <summary>
/// Extract Cohort Distribution Records without superseded nhs by nhs number first, if none found, get records with superseded by nhs number
/// </summary>
public class ExtractCohortDistributionRecords : IExtractCohortDistributionRecordsStrategy
{
    private readonly IDataServiceClient<CohortDistribution> _cohortDistributionDataServiceClient;

    public async Task<List<CohortDistribution>> GetUnextractedParticipants(int rowCount, bool retrieveSupersededRecordsLast)
    {
        // Try unextracted participants without superseded by nhs number first, if none found, get records with superseded by nhs number
        return await GetRegularUnextractedParticipants(rowCount)
            ?? await GetSupersededParticipants(rowCount);
    }

    public ExtractCohortDistributionRecords(IDataServiceClient<CohortDistribution> cohortDistributionDataServiceClient)
    {
        _cohortDistributionDataServiceClient = cohortDistributionDataServiceClient;
    }

    private async Task<List<CohortDistribution>?> GetRegularUnextractedParticipants(int rowCount)
    {
        var unextractedParticipants = await _cohortDistributionDataServiceClient.GetByFilter(
            x => x.IsExtracted.Equals(0) && x.RequestId == Guid.Empty && x.SupersededNHSNumber == null);

        return unextractedParticipants.Any()
            ? OrderAndTakeParticipants(unextractedParticipants, rowCount)
            : null;
    }

    private async Task<List<CohortDistribution>> GetSupersededParticipants(int rowCount)
    {
        var supersededParticipants = await _cohortDistributionDataServiceClient.GetByFilter(
            x => x.IsExtracted.Equals(0) && x.RequestId == Guid.Empty && x.SupersededNHSNumber != null);

        // Get distinct non-null superseded NHS numbers
        var supersededNhsNumbers = supersededParticipants
            .Select(sp => sp.SupersededNHSNumber.Value)
            .Distinct()
            .ToList();

        // Find matching extracted participants
        var matchingParticipants = new List<CohortDistribution>();
        foreach (var nhsNumber in supersededNhsNumbers)
        {
            var matches = await _cohortDistributionDataServiceClient.GetByFilter(
                x => x.NHSNumber == nhsNumber && x.IsExtracted.Equals(1));
            matchingParticipants.AddRange(matches);
        }

        // Filter superseded participants that have matching records
        var filteredParticipants = supersededParticipants
            .Where(sp => matchingParticipants.Any(mp => mp.NHSNumber == sp.SupersededNHSNumber))
            .ToList();

        return OrderAndTakeParticipants(filteredParticipants, rowCount);
    }

    private static List<CohortDistribution> OrderAndTakeParticipants(IEnumerable<CohortDistribution> participants, int rowCount)
    {
        return participants
            .OrderBy(x => (x.RecordUpdateDateTime ?? x.RecordInsertDateTime) ?? DateTime.MinValue)
            .Take(rowCount)
            .ToList();
    }
}
