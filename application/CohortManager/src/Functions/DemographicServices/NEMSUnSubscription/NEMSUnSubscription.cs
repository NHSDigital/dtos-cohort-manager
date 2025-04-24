namespace NHS.CohortManager.DemographicServices.NEMSUnSubscription;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using Azure.Data.Tables;
using Azure;
using Model;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Hl7.Fhir.Rest;
using Common;

public class NEMSUnSubscription
{
    private const string TableName = "NemsSubscriptionTable";
    protected readonly TableClient _tableClient;
    protected readonly HttpClient _httpClient;

    private readonly ILogger<NEMSUnSubscription> _logger;
    private readonly FhirJsonSerializer _fhirSerializer;
    private readonly FhirJsonParser _fhirParser;
    private readonly ICreateResponse _createResponse;
    private readonly IExceptionHandler _handleException;
    private readonly ICallFunction _callFunction;

    public interface IExceptionHandler
    {
        Task<HttpResponseData> HandleAsync(HttpRequestData req, HttpStatusCode statusCode, string message);
    }

    protected virtual Task<HttpResponseData> HandleNotFoundAsync(HttpRequestData req, string message)
    {
        return _handleException.HandleAsync(req, HttpStatusCode.NotFound, message);
    }

    // Default constructor (for runtime usage)


    public NEMSUnSubscription(ILogger<NEMSUnSubscription> logger,
    //IDataServiceClient<NemsSubscription> nemsSubscriptionClient, /* To Do Later */
    IHttpClientFactory httpClientFactory,
    IExceptionHandler handleException,
    ICreateResponse createResponse ,
    ICallFunction callFunction)
    {
        _logger = logger;
        _fhirSerializer = new FhirJsonSerializer();
        _fhirParser = new FhirJsonParser();
        _httpClient = httpClientFactory.CreateClient();
        _handleException = handleException;
        _createResponse = createResponse;
        _callFunction = callFunction;

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
            return await _handleException.HandleAsync(req, HttpStatusCode.NotFound, "No subscription record found.");
        }

        bool isDeletedFromNems = await DeleteSubscriptionFromNems(subscriptionId);

        if (!isDeletedFromNems)
        {
            logger.LogError("Failed to delete subscription from NEMS.");
            return await _handleException.HandleAsync(req, HttpStatusCode.BadGateway, "Failed to delete subscription from NEMS.");
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
    protected virtual async Task<bool> DeleteSubscriptionFromTableAsync(string nhsNumber)
    {
        try
        {
            await _tableClient.DeleteEntityAsync("SubscriptionPartition", nhsNumber);
            return true;
        }
        catch (RequestFailedException ex)
        {
            Console.WriteLine($"Error deleting from table: {ex.Message}");
            return false;
        }
    }
}
