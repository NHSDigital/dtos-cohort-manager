namespace NHS.CohortManager.DemographicServices;

using System.ComponentModel;
using System.Net;
using System.Text;
using System.Text.Json;
using Common;
using DataServices.Client;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging;
using Model;
using Model.Enums;
using RulesEngine.Models;

public class DurableDemographicFunction
{
    private readonly IDataServiceClient<ParticipantDemographic> _participantDemographic;

    private readonly ILogger<DurableDemographicFunction> _logger;

    private readonly ICreateResponse _createResponse;

    public DurableDemographicFunction(IDataServiceClient<ParticipantDemographic> dataServiceClient, ILogger<DurableDemographicFunction> logger, ICreateResponse createResponse)
    {
        _participantDemographic = dataServiceClient;
        _logger = logger;
        _createResponse = createResponse;
    }

    /// <summary>
    /// Orchestrates the execution of the Durable Demographic Function.
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    /// <exception cref="TimeoutException"></exception>
    [Function(nameof(DurableDemographicFunction))]
    public async Task<bool> RunOrchestrator(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var orchestrationTimeout = TimeSpan.FromHours(2.5);
        var expirationTime = context.CurrentUtcDateTime.Add(orchestrationTimeout);

        using (var cts = new CancellationTokenSource(orchestrationTimeout))
        {
            try
            {
                var demographicJsonData = context.GetInput<string>();

                var retryOptions = TaskOptions.FromRetryPolicy(new RetryPolicy(
                    maxNumberOfAttempts: 1, // this means the function will not retry and therefore add duplicates
                    firstRetryInterval: TimeSpan.FromSeconds(100))
                );

                // Add timeout-aware logic
                var task = context.CallActivityAsync<bool>(
                    nameof(InsertDemographicData),
                    demographicJsonData,
                    options: retryOptions
                );

                // Monitor for timeout
                var timeoutTask = context.CreateTimer(expirationTime, cts.Token);
                var completedTask = await Task.WhenAny(task, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    _logger.LogWarning("Orchestration timed out.");
                    throw new TimeoutException("Orchestration function exceeded its timeout.");
                }

                cts.Cancel();
                return await task;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Orchestration failed with exception.");
                return false;
            }
        }
    }

    /// <summary>
    /// Inserts demographic data into the data store.
    /// </summary>
    /// <param name="DemographicJsonData"></param>
    /// <param name="executionContext"></param>
    /// <returns></returns>
    [Function(nameof(InsertDemographicData))]
    public async Task<bool> InsertDemographicData([ActivityTrigger] string DemographicJsonData, FunctionContext executionContext)
    {
        try
        {
            var participantData = JsonSerializer.Deserialize<List<ParticipantDemographic>>(DemographicJsonData);
            var res = await _participantDemographic.AddRange(participantData);
            return res;

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Inserting demographic data failed");
            return false;
        }
    }


    /// <summary>
    /// Handles HTTP requests to initiate the Durable Demographic Function orchestration.
    /// </summary>
    /// <param name="req"></param>
    /// <param name="client"></param>
    /// <param name="executionContext"></param>
    /// <returns></returns>
    [Function("DurableDemographicFunction_HttpStart")]
    public async Task<HttpResponseData> HttpStart(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req,
        [DurableClient] DurableTaskClient client,
        FunctionContext executionContext)
    {
        try
        {
            // Function input comes from the request content.   
            var requestBody = "";
            using (StreamReader reader = new StreamReader(req.Body, Encoding.UTF8))
            {
                requestBody = await reader.ReadToEndAsync();
            }
            var instanceId = await client.ScheduleNewOrchestrationInstanceAsync(
                nameof(DurableDemographicFunction), requestBody, CancellationToken.None);

            _logger.LogInformation("Started orchestration with ID = '{instanceId}'.", instanceId);

            // Returns an HTTP 202 response the response status 
            return await client.CreateCheckStatusResponseAsync(req, instanceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "There has been an error executing the durable demographic function");
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req, ex.Message);
        }
    }


    [Function("GetOrchestrationStatus")]
    public async Task<HttpResponseData> GetOrchestrationStatus(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req,
    [DurableClient] DurableTaskClient client,
    FunctionContext executionContext)
    {
        var instanceId = "";
        var status = "";
        using (var reader = new StreamReader(req.Body, Encoding.UTF8))
        {
            var requestBodyJson = await reader.ReadToEndAsync();
            instanceId = JsonSerializer.Deserialize<string>(requestBodyJson);
        }

        if (!string.IsNullOrWhiteSpace(instanceId))
        {
            var instance = await client.GetInstanceAsync(instanceId);
            if (instance != null)
            {
                status = instance.RuntimeStatus.ToString();
            }
            else
            {
                // if the Orchestration is null then it means it means it's probably completed 
                status = OrchestrationRuntimeStatus.Completed.ToString();
            }
        }

        if (status == null)
        {
            _logger.LogWarning("No instance found with ID = {instanceId}", instanceId);
            return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req);
        }

        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

        // Write the status directly as a string
        await response.WriteStringAsync(status);
        return response;

    }

}
