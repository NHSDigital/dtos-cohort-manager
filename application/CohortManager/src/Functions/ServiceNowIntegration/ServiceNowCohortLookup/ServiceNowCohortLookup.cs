namespace NHS.CohortManager.ServiceNowIntegrationService;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Model;
using DataServices.Client;
using System.Linq;

public class ServiceNowCohortLookup
{
    private readonly ILogger<ServiceNowCohortLookup> _logger;
    private readonly IDataServiceClient<CohortDistribution> _cohortDistributionClient;
    private readonly IDataServiceClient<ServicenowCase> _serviceNowCasesClient;

    public ServiceNowCohortLookup(
        ILogger<ServiceNowCohortLookup> logger,
        IDataServiceClient<CohortDistribution> cohortDistributionClient,
        IDataServiceClient<ServicenowCase> serviceNowCasesClient)
    {
        _logger = logger;
        _cohortDistributionClient = cohortDistributionClient;
        _serviceNowCasesClient = serviceNowCasesClient;
    }

    /// <summary>
    /// Azure Function that run once per day to check whether the ServiceNow cases have been processed
    /// for auditing purposes.
    /// </summary>
    /// <param name="myTimer">Timer information from the Azure Functions host.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// This function runs once per day at midnight (as per the cron expression) and:
    /// 1. Retrieves new ServiceNow cases
    /// 2. Finds matching participants in cohort distribution which were added yesterday.
    /// 3. Updates case statuses for successful matches
    /// </remarks>
    [Function("ServiceNowCohortLookup")]
    public async Task Run([TimerTrigger("0 0 0 * * *")] TimerInfo myTimer)
    {
        _logger.LogInformation("ServiceNowCohortLookup function started at: {StartTime}", DateTime.UtcNow);

        try
        {
            var processingResult = await ProcessNewServiceNowCasesAsync();
            LogProcessingResult(processingResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process ServiceNow cohort lookup timer function");
        }

        LogNextSchedule(myTimer);
    }

    /// <summary>
    /// Processes all new ServiceNow cases and updates their status if matching participants are found.
    /// </summary>
    /// <returns>
    /// A tuple containing:
    /// <list type="bullet">
    /// <item><description>ProcessedCount: Number of successfully updated cases</description></item>
    /// <item><description>TotalCases: Total number of new cases processed</description></item>
    /// </list>
    /// </returns>
    private async Task<(int ProcessedCount, int TotalCases)> ProcessNewServiceNowCasesAsync()
    {
        var serviceNowCases = await GetNewServiceNowCasesAsync();
        if (serviceNowCases.Count == 0)
        {
            return (0, 0);
        }

        var participantsList = await GetYesterdayCohortParticipantsAsync();
        if (participantsList.Count == 0)
        {
            return (0, serviceNowCases.Count);
        }

        return await ProcessCasesAsync(serviceNowCases, participantsList);
    }

    /// <summary>
    /// Retrieves all new ServiceNow cases with status 'NEW'.
    /// </summary>
    /// <returns>List of new ServiceNow cases.</returns>
    private async Task<List<ServicenowCase>> GetNewServiceNowCasesAsync()
    {
        var cases = (await _serviceNowCasesClient.GetByFilter(c => c.Status == ServiceNowStatus.New)).ToList();
        _logger.LogInformation("Found {CaseCount} new ServiceNow cases", cases.Count);
        return cases;
    }

    /// <summary>
    /// Retrieves cohort participants that were inserted yesterday.
    /// </summary>
    /// <returns>List of cohort participants from yesterday.</returns>
    private async Task<List<CohortDistribution>> GetYesterdayCohortParticipantsAsync()
    {
        var yesterdayDate = DateTime.UtcNow.Date.AddDays(-1);
        var participants = (await _cohortDistributionClient.GetByFilter(c =>
            c.RecordInsertDateTime.HasValue &&
            c.RecordInsertDateTime.Value.Date == yesterdayDate))
            .ToList();

        _logger.LogInformation("Found {ParticipantCount} participants from {YesterdayDate}",
            participants.Count,
            yesterdayDate.ToString("dd-MM-yyyy"));

        return participants;
    }

    /// <summary>
    /// Processes a batch of ServiceNow cases against cohort participants.
    /// </summary>
    /// <param name="cases">List of ServiceNow cases to process.</param>
    /// <param name="participants">List of cohort participants to match against.</param>
    /// <returns>
    /// A tuple containing:
    /// <list type="bullet">
    /// <item><description>ProcessedCount: Number of successfully updated cases</description></item>
    /// <item><description>TotalCases: Total number of cases processed</description></item>
    /// </list>
    /// </returns>
    private async Task<(int ProcessedCount, int TotalCases)> ProcessCasesAsync(
        List<ServicenowCase> cases,
        List<CohortDistribution> participants)
    {
        var processedCount = 0;
        var participantLookup = CreateParticipantLookup(participants);

        foreach (var caseItem in cases)
        {
            try
            {
                if (await TryProcessCaseAsync(caseItem, participantLookup))
                {
                    processedCount++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to process ServiceNow case {ServiceNowId}", caseItem.ServicenowId);
            }
        }
        return (processedCount, cases.Count);
    }

    /// <summary>
    /// Creates a dictionary lookup for participants by NHS number.
    /// When multiple records exist for the same NHS number, selects the most recent one.
    /// </summary>
    private static Dictionary<long, CohortDistribution> CreateParticipantLookup(List<CohortDistribution> participants)
    {
        return participants
            .Where(p => p.NHSNumber != 0)
            .GroupBy(p => p.NHSNumber)
            .Select(g => g.OrderByDescending(p => p.RecordInsertDateTime).First())
            .ToDictionary(p => p.NHSNumber, p => p);
    }

    /// <summary>
    /// Attempts to process a single ServiceNow case.
    /// </summary>
    /// <param name="caseItem">The ServiceNow case to process.</param>
    /// <param name="participantLookup">Dictionary lookup of cohort participants by NHS number.</param>
    /// <returns>
    /// 'true' if the case was processed successfully; 'false' if processing failed due to:
    /// </returns>
    private async Task<bool> TryProcessCaseAsync(
        ServicenowCase caseItem,
        Dictionary<long, CohortDistribution> participantLookup)
    {
        if (!caseItem.NhsNumber.HasValue)
        {
            _logger.LogWarning("Case {ServiceNowId} has no NHS number", caseItem.ServicenowId);
            return false;
        }

        string nhsNumberString = Convert.ToString(caseItem.NhsNumber.Value);
        if (!long.TryParse(nhsNumberString, out long nhsNumber))
        {
            _logger.LogWarning("Invalid NHS number format: {NhsNumber}", nhsNumberString);
            return false;
        }

        if (!participantLookup.TryGetValue(nhsNumber, out var participant))
        {
            _logger.LogInformation("No participant found for NHS number in ServiceNowId {ServicenowId}", caseItem.ServicenowId);
            return false;
        }

        return await UpdateCaseStatusAsync(caseItem);
    }

    /// <summary>
    /// Updates the status of a ServiceNow case to 'Complete'.
    /// </summary>
    /// <param name="caseItem">The case to update.</param>
    /// <returns>'true' if the update was successful; otherwise, 'false'.</returns>
    private async Task<bool> UpdateCaseStatusAsync(ServicenowCase caseItem)
    {
        caseItem.Status = ServiceNowStatus.Complete;
        caseItem.RecordUpdateDatetime = DateTime.UtcNow;

        if (!await _serviceNowCasesClient.Update(caseItem))
        {
            throw new InvalidOperationException($"Failed to update status for case {caseItem.ServicenowId}");
        }

        _logger.LogInformation("Updated case {ServiceNowId} to {Status}",
            caseItem.ServicenowId,
            ServiceNowStatus.Complete);

        return true;
    }

    private void LogProcessingResult((int ProcessedCount, int TotalCases) result)
    {
        _logger.LogInformation(
            "Processed {ProcessedCount}/{TotalCases} cases successfully",
            result.ProcessedCount,
            result.TotalCases);
    }

    /// <summary>
    /// Logs the next scheduled execution time.
    /// </summary>
    /// <param name="timer">Timer information containing schedule details.</param>
    private void LogNextSchedule(TimerInfo timer)
    {
        if (timer.ScheduleStatus is not null)
        {
            _logger.LogInformation("Next execution scheduled for: {NextRun}",
                timer.ScheduleStatus.Next);
        }
    }
}
