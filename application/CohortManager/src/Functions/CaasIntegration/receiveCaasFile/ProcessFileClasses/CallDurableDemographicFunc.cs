namespace NHS.Screening.ReceiveCaasFile;


using System.Text.Json;
using Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Model;
using Model.Enums;
using Polly;

public class CallDurableDemographicFunc : ICallDurableDemographicFunc
{

    private readonly IHttpClientFunction _httpClientFunction;
    private readonly ILogger<CallDurableDemographicFunc> _logger;
    private readonly HttpClient _httpClient;
    private readonly ICopyFailedBatchToBlob _copyFailedBatchToBlob;

    private readonly IExceptionHandler _exceptionHandler;
    private readonly int _maxNumberOfChecks;
    private TimeSpan _delayBetweenChecks = TimeSpan.FromSeconds(3);

    private readonly ReceiveCaasFileConfig _config;


    public CallDurableDemographicFunc(IHttpClientFunction httpClientFunction, ILogger<CallDurableDemographicFunc> logger, HttpClient httpClient, ICopyFailedBatchToBlob copyFailedBatchToBlob, IExceptionHandler exceptionHandler, IOptions<ReceiveCaasFileConfig> config)
    {
        _config = config.Value;
        _httpClientFunction = httpClientFunction;
        _logger = logger;
        _httpClient = httpClient;
        _exceptionHandler = exceptionHandler;

        _copyFailedBatchToBlob = copyFailedBatchToBlob;
        _maxNumberOfChecks = _config.maxNumberOfChecks;
        _httpClient.Timeout = TimeSpan.FromSeconds(300);
    }

    /// <summary>
    /// Posts demographic data for a list of participants to the specified demographic function URI.
    /// </summary>
    /// <param name="participants">A list of <see cref="ParticipantDemographic"/> objects representing the participants to send.</param>
    /// <param name="DemographicFunctionURI">The URI of the demographic function to post data to.</param>
    /// <returns>
    /// A task representing the asynchronous operation. Returns true if the operation completes successfully or if no participants were provided.
    /// </returns>
    /// <remarks>
    /// This method handles posting data, logging, and checking the status of the durable function.
    /// Implements retry logic for status checking.
    /// </remarks>
    public async Task<bool> PostDemographicDataAsync(List<ParticipantDemographic> participants, string DemographicFunctionURI, string fileName)
    {
        var batchSize = participants.Count;
        var responseContent = "";
        if (participants.Count == 0)
        {
            _logger.LogInformation("There were no items to to send to the demographic durable function");
            return true;
        }

        try
        {
            var content = JsonSerializer.Serialize(participants);
            var response = await _httpClientFunction.SendPost(DemographicFunctionURI, content);

            responseContent = response.Headers.Location!.ToString();
            // This is not retrying the function if it fails but checking if it has done yet.
            var retryPolicy = Policy
                .HandleResult<WorkFlowStatus>(status => status != WorkFlowStatus.Completed && status != WorkFlowStatus.Failed)
                .WaitAndRetryAsync(_maxNumberOfChecks, check => _delayBetweenChecks,
                    (result, timeSpan, checkCount, context) =>
                    {
                        _logger.LogWarning("Status: {Result}, checking status: ({CheckCount} / {MaxNumberOfChecks})...", result.Result, checkCount, _maxNumberOfChecks);
                    });

            var finalStatus = await retryPolicy.ExecuteAsync(async () =>
            {
                return await GetStatus(responseContent);
            });

            if (finalStatus == WorkFlowStatus.Completed)
            {
                _logger.LogWarning("Durable function completed {FinalStatus}", finalStatus);
                return true;
            }
            else
            {
                _logger.LogError("Check limit reached or demographic function failed for a batch of size: {BatchSize} {FinalStatus}", batchSize, finalStatus);
                await _copyFailedBatchToBlob.writeBatchToBlob(
                    JsonSerializer.Serialize(participants),
                    new InvalidOperationException("There was an error while adding batch of participants to the demographic table")
                );

                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred: {Message}. not sending {BatchSize} records to queue", ex.Message, batchSize);
            //we process the participant record here in batch so don't have a single record or know why a single record has failed
            await _exceptionHandler.CreateSystemExceptionLog(ex, new Participant(), fileName);

            await _copyFailedBatchToBlob.writeBatchToBlob(
                JsonSerializer.Serialize(participants),
                new InvalidOperationException("there was an error while adding batch of participants to the demographic table")
            );

            return false;
        }
    }

    /// <summary>
    /// Checks the status of a workflow using the provided status request URI.
    /// </summary>
    /// <param name="statusRequestGetUri">The URI to request the workflow status.</param>
    /// <returns>
    /// A task representing the asynchronous operation. Returns the <see cref="WorkFlowStatus"/> of the workflow.
    /// </returns>
    /// <remarks>
    /// Logs errors if the status cannot be parsed or if the response contains unexpected data.
    /// </remarks>
    private async Task<WorkFlowStatus> GetStatus(string statusRequestGetUri)
    {
        try
        {
            // We are not calling a function that we have written here we are calling a webhook that is created by the durable demographic function
            using HttpResponseMessage response = await _httpClient.GetAsync(statusRequestGetUri);
            var jsonResponse = await response.Content.ReadAsStringAsync();

            var data = JsonSerializer.Deserialize<WebhookResponse>(jsonResponse);

            if (data != null)
            {
                if (!Enum.TryParse(data.RuntimeStatus, out WorkFlowStatus workFlowStatus))
                {
                    _logger.LogError(jsonResponse);
                }
                return workFlowStatus;
            }
            return WorkFlowStatus.Unknown;
        }
        //sometimes the webhook can fail for very annoying reasons but the Orchestration data still exists so we can check for its status
        catch (Exception ex)
        {
            var getOrchestrationStatusURL = _config.GetOrchestrationStatusURL;

            if (string.IsNullOrWhiteSpace(getOrchestrationStatusURL))
            {
                _logger.LogError("The GetOrchestrationStatusURL was not found");
            }

            var instanceId = getInstanceId(statusRequestGetUri);
            _logger.LogWarning(ex, "There has been error getting the status for instanceId {InstanceId}", instanceId);

            var json = JsonSerializer.Serialize(instanceId);
            var response = await _httpClientFunction.SendPost(getOrchestrationStatusURL, json);
            var responseBody = await _httpClientFunction.GetResponseText(response);

            if (Enum.TryParse<WorkFlowStatus>(responseBody, out var result))
            {
                _logger.LogWarning("Recovered from an error while getting the status for a Orchestration. Status was: {Status}", result);
                return result;
            }
        }

        return WorkFlowStatus.Unknown;
    }

    private static string getInstanceId(string statusRequestGetUri)
    {
        var splitStringList = statusRequestGetUri.Split('/').ToList();
        return splitStringList[7].Split('?')[0];
    }

}

