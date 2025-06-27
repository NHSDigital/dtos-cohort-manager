namespace NHS.CohortManager.CohortDistributionServices;

using System.Net;
using System.Text.Json;
using Common;
using DataServices.Client;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Model;
using Model.Enums;

public class DistributeParticipantActivities
{
    private readonly IDataServiceClient<CohortDistribution> _cohortDistributionClient;
    private readonly IDataServiceClient<ParticipantManagement> _participantManagementClient;
    private readonly DistributeParticipantConfig _config;
    private readonly IHttpClientFunction _httpClient;
    private readonly ILogger<DistributeParticipantActivities> _logger;

    public DistributeParticipantActivities(IDataServiceClient<CohortDistribution> cohortDistributionClient,
                                           IDataServiceClient<ParticipantManagement> participantManagementClient,
                                           IOptions<DistributeParticipantConfig> config,
                                           IHttpClientFunction httpClientFunction,
                                           ILogger<DistributeParticipantActivities> logger)
    {
        _cohortDistributionClient = cohortDistributionClient;
        _participantManagementClient = participantManagementClient;
        _config = config.Value;
        _httpClient = httpClientFunction;
        _logger = logger;
    }


    // TODO: make sure all activities are idempotent
    /// <summary>
    /// Calls retrieve participant data which constructs a CohortDistributionParticipant
    /// based on the data from the Participant Management and Demographic tables.
    /// </summary>
    /// <returns>
    /// CohortDistributionParticipant, or null if there were any exceptions during execution.
    /// </returns>
    [Function(nameof(RetrieveParticipantData))]
    public async Task<CohortDistributionParticipant> RetrieveParticipantData(BasicParticipantData participantData)
    {
        // TODO: if response = OK but data is null, return exception and do not continue processing
        long nhsNumber = long.Parse(participantData.NhsNumber);
        long screeningId = long.Parse(participantData.ScreeningId);

        // Get participant management data
        var participantManagement = await _participantManagementClient.GetSingleByFilter(p => p.NHSNumber == nhsNumber &&
                                                                                        p.ScreeningId == screeningId);

        if (participantManagement is null)
        {
            throw new KeyNotFoundException("Could not find participant in the participant management table");
        }

        // Get demographic data
        Dictionary<string, string> demographicFunctionParams = new() { { "Id", participantData.NhsNumber } };

        var demographicDataJson = await _httpClient.SendGet(_config.DemographicDataFunctionURL, demographicFunctionParams)
            ?? throw new HttpRequestException("Demographic request failed");

        var demographicData = JsonSerializer.Deserialize<Demographic>(demographicDataJson);

        // Create cohort distribution participant
        CohortDistributionParticipant participant = new(participantManagement, demographicData)
        {
            //TODO, This needs to happen elsewhere Hardcoded for now
            ScreeningName = "Breast Screening",
            ScreeningAcronym = "BSS"
        };

        return participant;
    }

    [Function(nameof(AllocateServiceProvider))]
    public async Task<string?> AllocateServiceProvider(string screeningAcronym, string postCode)
    {
        string configFilePath = Path.Combine(Environment.CurrentDirectory, "AllocateServiceProvider", "allocationConfig.json");

        string configFile = await File.ReadAllTextAsync(configFilePath);
        var allocationConfigEntries = JsonSerializer.Deserialize<AllocationConfigDataList>(configFile);

        string serviceProvider = allocationConfigEntries.ConfigDataList
            .Where(item => postCode.StartsWith(item.Postcode, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(item.ScreeningService, screeningAcronym, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(item => item.Postcode.Length)
            .Select(item => item.ServiceProvider)
            .FirstOrDefault() ?? "BS SELECT";

        return serviceProvider;
    }

    [Function(nameof(AddParticipant))]
    public async Task<bool> AddParticipant(CohortDistributionParticipant transformedParticipant)
    {
        transformedParticipant.Extracted = Convert.ToInt32(_config.IsExtractedToBSSelect).ToString();
        var cohortDistributionParticipantToAdd = transformedParticipant.ToCohortDistribution();
        var isAdded = await _cohortDistributionClient.Add(cohortDistributionParticipantToAdd);

        _logger.LogInformation("sent participant to cohort distribution data service");
        return isAdded;
    }
}