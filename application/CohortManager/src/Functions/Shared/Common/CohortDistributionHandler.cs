namespace Common;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Model;
using NHS.CohortManager.CohortDistribution;

public class CohortDistributionHandler : ICohortDistributionHandler
{

    private readonly ILogger<CohortDistributionHandler> _logger;
    private readonly ICallFunction _callFunction;
    public CohortDistributionHandler(ILogger<CohortDistributionHandler> logger, ICallFunction callFunction)
    {
        _logger = logger;
        _callFunction = callFunction;
    }

    public async Task<bool> SendToCohortDistributionService(string nhsNumber, string screeningService, string recordType, string fileName, string errorRecord)
    {
        CreateCohortDistributionRequestBody requestBody = new CreateCohortDistributionRequestBody
        {
            NhsNumber = nhsNumber,
            ScreeningService = screeningService,
            FileName = fileName,
            RecordType = recordType,
            ErrorRecord = errorRecord
        };
        string json = JsonSerializer.Serialize(requestBody);

        var result = await _callFunction.SendPost(Environment.GetEnvironmentVariable("CohortDistributionServiceURL"), json);

        if (result.StatusCode == HttpStatusCode.OK)
        {
            _logger.LogInformation($"Participant sent to Cohort Distribution Service");
            return true;
        }
        _logger.LogWarning("Unable to send participant to Cohort Distribution Service");
        return false;
    }
}
