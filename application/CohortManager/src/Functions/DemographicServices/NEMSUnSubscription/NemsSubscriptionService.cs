namespace NHS.CohortManager.DemographicServices.NEMSUnSubscription;

using System;
using System.Linq;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using Microsoft.Extensions.Options;
using NHS.Screening.NEMSUnSubscription;

public interface INemsSubscriptionService
{
    Task<string?> LookupSubscriptionIdAsync(string nhsNumber);
    Task<bool> DeleteSubscriptionFromNems(string subscriptionId);
    Task<bool> DeleteSubscriptionFromTableAsync(string nhsNumber);
}

public class NemsSubscriptionService : INemsSubscriptionService
{
    private readonly TableClient _tableClient;
    private readonly HttpClient _httpClient;
    private readonly ILogger<NemsSubscriptionService> _logger;
   private readonly NEMSUnSubscriptionConfig _config;

    public NemsSubscriptionService(
        TableClient tableClient,
        HttpClient httpClient,
        IOptions<NEMSUnSubscriptionConfig> nemsUnSubscriptionConfig,
        ILogger<NemsSubscriptionService> logger)
    {
        _tableClient = tableClient;
        _httpClient = httpClient;
        _config = nemsUnSubscriptionConfig.Value;
        _logger = logger;
    }

    public async Task<string?> LookupSubscriptionIdAsync(string nhsNumber)
    {
        try
        {
            Pageable<TableEntity> queryResults = _tableClient.Query<TableEntity>(e => e.RowKey == nhsNumber);
            var entity = queryResults.FirstOrDefault();
            return entity?.GetString("SubscriptionId");
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Error querying table for NHS number {NhsNumber}", nhsNumber);
            return null;
        }
    }

    public async Task<bool> DeleteSubscriptionFromNems(string subscriptionId)
    {
        try
        {
            string nemsEndpoint = _config.NemsDeleteEndpoint;
            var response = await _httpClient.DeleteAsync($"{nemsEndpoint}/{subscriptionId}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling NEMS API to delete subscription ID {SubscriptionId}", subscriptionId);
            return false;
        }
    }

    public async Task<bool> DeleteSubscriptionFromTableAsync(string nhsNumber)
    {
        try
        {
            await _tableClient.DeleteEntityAsync("SubscriptionPartition", nhsNumber);
            return true;
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Error deleting NHS number {NhsNumber} from Azure Table", nhsNumber);
            return false;
        }
    }
}
