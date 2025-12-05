namespace NHS.CohortManager.CohortDistributionServices;

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
    private readonly IDataServiceClient<ParticipantDemographic> _participantDemographicClient;
    private readonly IDataServiceClient<ServicenowCase> _serviceNowCasesClient;
    private readonly DistributeParticipantConfig _config;
    private readonly ILogger<DistributeParticipantActivities> _logger;
    private readonly IHttpClientFunction _httpClientFunction;

    public DistributeParticipantActivities(IDataServiceClient<CohortDistribution> cohortDistributionClient,
                                           IDataServiceClient<ParticipantManagement> participantManagementClient,
                                           IDataServiceClient<ParticipantDemographic> participantDemographicClient,
                                           IDataServiceClient<ServicenowCase> serviceNowCasesClient,
                                           IOptions<DistributeParticipantConfig> config,
                                           ILogger<DistributeParticipantActivities> logger,
                                           IHttpClientFunction httpClientFunction)
    {
        _cohortDistributionClient = cohortDistributionClient;
        _participantManagementClient = participantManagementClient;
        _participantDemographicClient = participantDemographicClient;
        _serviceNowCasesClient = serviceNowCasesClient;
        _config = config.Value;
        _logger = logger;
        _httpClientFunction = httpClientFunction;
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
        var participantManagementTask = _participantManagementClient.GetSingleByFilter(p => p.NHSNumber == nhsNumber &&
                                                                                        p.ScreeningId == screeningId);

        // Get participant demographic data
        var participantDemographicTask = _participantDemographicClient.GetSingleByFilter(p => p.NhsNumber == nhsNumber);

        ParticipantManagement participantManagement = await participantManagementTask;
        ParticipantDemographic participantDemographic = await participantDemographicTask;

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
        if (string.IsNullOrEmpty(participant.Postcode) || string.IsNullOrEmpty(participant.ScreeningAcronym))
        {
            return EnumHelper.GetDisplayName(ServiceProvider.BSS);
        }

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

        newRecord.RecordUpdateDateTime = newRecord.RecordInsertDateTime = DateTime.UtcNow;
        var isAdded = await _cohortDistributionClient.Add(newRecord);

        _logger.LogInformation("sent participant to cohort distribution data service");
        return isAdded;
    }

    /// <summary>
    /// Sends a success message to the ServiceNow for the participant
    /// </summary>
    /// <param name="serviceNowCaseNumber"></param>
    [Function(nameof(SendServiceNowMessage))]
    public async Task SendServiceNowMessage([ActivityTrigger] string serviceNowCaseNumber)
    {
        var url = $"{_config.SendServiceNowMessageURL}/{serviceNowCaseNumber}";
        var requestBody = new SendServiceNowMessageRequestBody
        {
            MessageType = ServiceNowMessageType.Success
        };
        var json = JsonSerializer.Serialize(requestBody);

        await _httpClientFunction.SendPut(url, json);
    }

    /// <summary>
    /// Updates the ServiceNow case status to 'Complete' in the database.
    /// Handles multiple rows with the same ServiceNowId (when ServiceNowId is not the primary key).
    /// </summary>
    /// <param name="serviceNowCaseNumber"></param>
    [Function(nameof(UpdateServiceNowCaseStatus))]
    public async Task<bool> UpdateServiceNowCaseStatus([ActivityTrigger] string serviceNowCaseNumber)
    {
        try
        {
            var serviceNowCases = await _serviceNowCasesClient.GetByFilter(c => c.ServicenowId == serviceNowCaseNumber);

            if (serviceNowCases == null || !serviceNowCases.Any())
            {
                _logger.LogWarning("ServiceNow case {ServiceNowId} not found in database", serviceNowCaseNumber);
                return false;
            }

            var updateTasks = serviceNowCases.Select(async serviceNowCase =>
            {
                serviceNowCase.Status = ServiceNowStatus.Complete;
                serviceNowCase.RecordUpdateDatetime = DateTime.UtcNow;
                return await _serviceNowCasesClient.Update(serviceNowCase);
            });

            var results = await Task.WhenAll(updateTasks);
            var allUpdated = results.All(r => r);

            if (allUpdated)
            {
                _logger.LogInformation("Updated {Count} ServiceNow case record(s) {ServiceNowId} status to {Status}",
                    serviceNowCases.Count(), serviceNowCaseNumber, ServiceNowStatus.Complete);
            }
            else
            {
                _logger.LogError("Failed to update some ServiceNow case {ServiceNowId} records. {SuccessCount}/{TotalCount} succeeded",
                    serviceNowCaseNumber, results.Count(r => r), results.Length);
            }

            return allUpdated;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating ServiceNow case {ServiceNowId} status", serviceNowCaseNumber);
            return false;
        }
    }
}
