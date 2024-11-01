namespace Common;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Model;
using NHS.CohortManager.CohortDistribution;

public class CohortDistributionHandler : ICohortDistributionHandler
{

    private readonly ILogger<CohortDistributionHandler> _logger;
    private readonly IAzureQueueStorageHelper _azureQueueStorageHelper;

    public CohortDistributionHandler(ILogger<CohortDistributionHandler> logger, IAzureQueueStorageHelper azureQueueStorageHelper)
    {
        _logger = logger;
        _azureQueueStorageHelper = azureQueueStorageHelper;
    }

    public async Task<bool> SendToCohortDistributionService(string nhsNumber, string screeningService, string recordType, string fileName, Participant errorRecord)
    {
        CreateCohortDistributionRequestBody requestBody = new CreateCohortDistributionRequestBody
        {
            NhsNumber = nhsNumber,
            ScreeningService = screeningService,
            FileName = fileName,
            RecordType = recordType,
            ErrorRecord = errorRecord
        };

        await _azureQueueStorageHelper.AddItemToQueueAsync<CreateCohortDistributionRequestBody>(requestBody, "create-cohort-distribution-queue");

        _logger.LogInformation($"Participant sent to Cohort Distribution Service");
        return true;
    }
}
