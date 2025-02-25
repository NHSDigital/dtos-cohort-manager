namespace NHS.Screening.ReceiveCaasFile;


using System.Net.Http.Headers;
using System.Text.Json;
using Common;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;
using Microsoft.Extensions.Logging;
using Model;
using Model.Enums;
using Polly;

public class CallDurableDemographicFunc : ICallDurableDemographicFunc
{

    private readonly ICallFunction _callFunction;
    private readonly ILogger<CallDurableDemographicFunc> _logger;
    private readonly HttpClient _httpClient;

    private readonly ICopyFailedBatchToBlob _copyFailedBatchToBlob;

    private const int _maxNumberOfChecks = 50;
    private TimeSpan _delayBetweenChecks = TimeSpan.FromSeconds(3);


    public CallDurableDemographicFunc(ICallFunction callFunction, ILogger<CallDurableDemographicFunc> logger, HttpClient httpClient, ICopyFailedBatchToBlob copyFailedBatchToBlob)
    {
        _callFunction = callFunction;
        _logger = logger;
        _httpClient = httpClient;
        _copyFailedBatchToBlob = copyFailedBatchToBlob;

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
    public async Task<bool> PostDemographicDataAsync(List<ParticipantDemographic> participants, string DemographicFunctionURI)
    {
        var responseContent = "";
        if (participants.Count == 0)
        {
            _logger.LogInformation("There were no items to to send to the demographic durable function");
            return true;
        }

        try
        {
            using var memoryStream = new MemoryStream();
            // this seems to be better for memory management
            await JsonSerializer.SerializeAsync(memoryStream, participants);
            memoryStream.Position = 0;

            var content = new StreamContent(memoryStream);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            var response = await _httpClient.PostAsync(DemographicFunctionURI, content);

            responseContent = response.Headers.Location.ToString();

            // this is not retrying the function if it fails but checking if it has done yet.
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
                _logger.LogWarning("Durable function completed {finalStatus}", finalStatus);
                return true;
            }
            else
            {
                _logger.LogError("Check limit reached or demographic function failed {finalStatus}", finalStatus);
                await _copyFailedBatchToBlob.writeBatchToBlob(
                    JsonSerializer.Serialize(participants),
                    new InvalidOperationException("there was an error while adding batch of participants to the demographic table")
                );

                return false;
            }
        }
        catch (Exception ex)
        {
            // we want to do this as we don't want to lose records
            _logger.LogError(ex, "An error occurred: {Message} still sending records to queue", ex.Message);
            return true;
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
            var getOrchestrationStatusURL = Environment.GetEnvironmentVariable("GetOrchestrationStatusURL");

            if (string.IsNullOrWhiteSpace(getOrchestrationStatusURL))
            {
                _logger.LogError("The GetOrchestrationStatusURL was not found");
            }

            var instanceId = getInstanceId(statusRequestGetUri);
            _logger.LogWarning(ex, "There has been error getting the status for instanceId {InstanceId}", instanceId);

            var json = JsonSerializer.Serialize(instanceId);
            var response = await _callFunction.SendPost(getOrchestrationStatusURL, json);
            var responseBody = await _callFunction.GetResponseText(response);

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

        for (int i = 1; i < splitStringList.Count; i++)
        {
            //find instance id in URL
            if (splitStringList[i - 1] == "instances")
            {
                return splitStringList[i].Split('?')[0];
            }
        }

        return "";
    }

}

