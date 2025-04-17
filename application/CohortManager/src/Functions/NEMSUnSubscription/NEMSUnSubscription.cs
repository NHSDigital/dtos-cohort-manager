namespace NHS.CohortManager.NEMSUnSubscription;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using Azure.Data.Tables;
using Azure;
using Model;

public class NEMSUnSubscription
{
    private const string TableName = "NemsSubscriptionTable";
    protected readonly TableClient _tableClient;
    protected readonly HttpClient _httpClient;

    // Default constructor (for runtime usage)
    public NEMSUnSubscription()
    {
        var storageUri = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
        _tableClient = new TableClient(storageUri, TableName);
        _httpClient = new HttpClient();
    }

    // Constructor for dependency injection (testability)
    public NEMSUnSubscription(TableClient tableClient, HttpClient httpClient)
    {
        _tableClient = tableClient;
        _httpClient = httpClient;
    }

    [Function("NEMSUnsubscribe")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req,
        FunctionContext executionContext)
    {
        var logger = executionContext.GetLogger("NEMSUnsubscribe");

        var requestBody = await new StreamReader(req.Body).ReadToEndAsync();

        if (string.IsNullOrWhiteSpace(requestBody))
        {
            var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await badRequestResponse.WriteStringAsync("Request body is empty.");
            return badRequestResponse;
        }

        var request = JsonSerializer.Deserialize<UnsubscriptionRequest>(requestBody);

        if (request == null || string.IsNullOrEmpty(request.NhsNumber))
        {
            var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
            await badRequest.WriteStringAsync("Invalid or missing NHS number.");
            return badRequest;
        }

        string nhsNumber = request.NhsNumber;
        logger.LogInformation($"Received NHS Number: {nhsNumber}");

        string? subscriptionId = await LookupSubscriptionIdAsync(nhsNumber);

        if (string.IsNullOrEmpty(subscriptionId))
        {
            logger.LogWarning("No subscription record found.");
            var errorResponse = req.CreateResponse(HttpStatusCode.NotFound);
            await errorResponse.WriteStringAsync("No subscription record found.");
            return errorResponse;
        }

        bool isDeletedFromNems = await DeleteSubscriptionFromNems(subscriptionId);

        if (!isDeletedFromNems)
        {
            logger.LogError("Failed to delete subscription from NEMS.");
            var errorResponse = req.CreateResponse(HttpStatusCode.BadGateway);
            await errorResponse.WriteStringAsync("Failed to delete subscription from NEMS.");
            return errorResponse;
        }

        await DeleteSubscriptionFromTableAsync(nhsNumber);
        logger.LogInformation("Subscription deleted successfully.");

        var successResponse = req.CreateResponse(HttpStatusCode.OK);
        await successResponse.WriteStringAsync("Successfully unsubscribed.");
        return successResponse;
    }

    /// <summary>
    /// Looks up the subscription ID based on NHS number.
    /// </summary>
    protected virtual async Task<string?> LookupSubscriptionIdAsync(string nhsNumber)
    {
        try
        {
            Pageable<TableEntity> queryResults = _tableClient.Query<TableEntity>(e => e.RowKey == nhsNumber);
            var entity = queryResults.FirstOrDefault();
            return entity?.GetString("SubscriptionId");
        }
        catch (RequestFailedException ex)
        {
            Console.WriteLine($"Error querying table: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Sends delete request to NEMS API.
    /// </summary>
    protected virtual async Task<bool> DeleteSubscriptionFromNems(string subscriptionId)
    {
        try
        {
            string nemsEndpoint = Environment.GetEnvironmentVariable("NemsDeleteEndpoint");
            var response = await _httpClient.DeleteAsync($"{nemsEndpoint}/{subscriptionId}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error calling NEMS API: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Deletes the subscription ID from the Azure Table.
    /// </summary>
    protected virtual async Task DeleteSubscriptionFromTableAsync(string nhsNumber)
    {
        try
        {
            await _tableClient.DeleteEntityAsync("SubscriptionPartition", nhsNumber);
        }
        catch (RequestFailedException ex)
        {
            Console.WriteLine($"Error deleting from table: {ex.Message}");
        }
    }
}
