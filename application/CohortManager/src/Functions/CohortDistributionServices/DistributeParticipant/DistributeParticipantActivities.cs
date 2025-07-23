namespace NHS.CohortManager.CohortDistributionServices;

using System.Text.Json;
using DataServices.Client;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Model;

public class DistributeParticipantActivities
{
    private readonly IDataServiceClient<CohortDistribution> _cohortDistributionClient;
    private readonly IDataServiceClient<ParticipantManagement> _participantManagementClient;
    private readonly IDataServiceClient<ParticipantDemographic> _participantDemographicClient;
    private readonly DistributeParticipantConfig _config;
    private readonly ILogger<DistributeParticipantActivities> _logger;

    public DistributeParticipantActivities(IDataServiceClient<CohortDistribution> cohortDistributionClient,
                                           IDataServiceClient<ParticipantManagement> participantManagementClient,
                                           IDataServiceClient<ParticipantDemographic> participantDemographicClient,
                                           IOptions<DistributeParticipantConfig> config,
                                           ILogger<DistributeParticipantActivities> logger)
    {
        _cohortDistributionClient = cohortDistributionClient;
        _participantManagementClient = participantManagementClient;
        _participantDemographicClient = participantDemographicClient;
        _config = config.Value;
        _logger = logger;
    }

    /// <summary>
    /// Constructs a <see cref="CohortDistributionParticipant"/> based on the data
    /// from the participant management and demographic tables
    /// </summary>
    /// <param name="participantData"> participant data containing the NHS Number and Screening ID which are used to query the tables</param>
    /// <returns>
    /// <see cref="CohortDistributionParticipant"/>, or null if the participant could not be found in either table.
    /// </returns>
    [Function(nameof(RetrieveParticipantData))]
    public async Task<CohortDistributionParticipant?> RetrieveParticipantData([ActivityTrigger] BasicParticipantData participantData)
    {
        long nhsNumber = long.Parse(participantData.NhsNumber);
        long screeningId = long.Parse(participantData.ScreeningId);

        // Get participant management data
        var participantManagement = await _participantManagementClient.GetSingleByFilter(p => p.NHSNumber == nhsNumber &&
                                                                                        p.ScreeningId == screeningId);

        // Get participant demographic data
        var participantDemographic = await _participantDemographicClient.GetSingleByFilter(p => p.NhsNumber == nhsNumber);

        if (participantDemographic is null || participantManagement is null)
        {
            return null;
        }

        // Create cohort distribution participant
        CohortDistributionParticipant participant = new(participantManagement, participantDemographic)
        {
            RecordType = participantData.RecordType,
            //TODO, This needs to happen elsewhere Hardcoded for now
            ScreeningName = "Breast Screening",
            ScreeningAcronym = "BSS"
        };

        return participant;
    }

    /// <summary>
    /// Allocates the participant to a service provider based on the postcode area (1st part of the outcode)
    /// </summary>
    /// <param name="participant"></param>
    /// <returns>A string representing the service provider</returns>
    [Function(nameof(AllocateServiceProvider))]
    public async Task<string> AllocateServiceProvider([ActivityTrigger] Participant participant)
    {
        string configFilePath = Path.Combine(Environment.CurrentDirectory, "AllocateServiceProvider", "allocationConfig.json");
        string configFile = await File.ReadAllTextAsync(configFilePath);

        var allocationConfigEntries = JsonSerializer.Deserialize<AllocationConfigDataList>(configFile);

        string serviceProvider = allocationConfigEntries.ConfigDataList
            .Where(item => participant.Postcode.StartsWith(item.Postcode, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(item.ScreeningService, participant.ScreeningAcronym, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(item => item.Postcode.Length)
            .Select(item => item.ServiceProvider)
            .FirstOrDefault() ?? "BS SELECT";

        return serviceProvider;
    }

    /// <summary>
    /// Adds the participant to the cohort distribution table
    /// </summary>
    /// <param name="transformedParticipant"></param>
    /// <returns>bool, whether or not the add was successful</returns>
    [Function(nameof(AddParticipant))]
    public async Task<bool> AddParticipant([ActivityTrigger] CohortDistributionParticipant transformedParticipant)
    {
        transformedParticipant.Extracted = Convert.ToInt32(_config.IsExtractedToBSSelect).ToString();
        var newRecord = transformedParticipant.ToCohortDistribution();

        if (newRecord.RecordInsertDateTime is null)
        {
            newRecord.RecordInsertDateTime = DateTime.UtcNow;
        }
        newRecord.RecordUpdateDateTime = DateTime.UtcNow;
        var isAdded = await _cohortDistributionClient.Add(newRecord);

        _logger.LogInformation("sent participant to cohort distribution data service");
        return isAdded;
    }
}