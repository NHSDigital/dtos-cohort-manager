namespace Common;

using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Model;
using Model.Enums;
using Polly;

public class CheckDemographic : ICheckDemographic
{
    private readonly ICallFunction _callFunction;
    private readonly ILogger<CheckDemographic> _logger;
    private readonly HttpClient _httpClient;

    private const int maxNumberOfChecks = 50;
    private TimeSpan delayBetweenChecks = TimeSpan.FromSeconds(3);


    public CheckDemographic(ICallFunction callFunction, ILogger<CheckDemographic> logger, HttpClient httpClient)
    {
        _callFunction = callFunction;
        _logger = logger;
        _httpClient = httpClient;

        _httpClient.Timeout = TimeSpan.FromSeconds(300);
    }

    /// <summary>
    /// Retrieves demographic information for a specific NHS number.
    /// </summary>
    /// <param name="NhsNumber"></param>
    /// <param name="DemographicFunctionURI"></param>
    /// <returns></returns>

    public async Task<Demographic> GetDemographicAsync(string NhsNumber, string DemographicFunctionURI)
    {
        var url = $"{DemographicFunctionURI}?Id={NhsNumber}";

        var response = await _callFunction.SendGet(url);
        var demographicData = JsonSerializer.Deserialize<Demographic>(response);

        return demographicData;
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
                .HandleResult<WorkFlowStatus>(status => status != WorkFlowStatus.Completed)
                .WaitAndRetryAsync(maxNumberOfChecks, check => delayBetweenChecks,
                    (result, timeSpan, checkCount, context) =>
                    {
                        _logger.LogWarning("Status: {result}, checking status: ({checkCount} / {maxNumberOfChecks})...", result.Result, checkCount, maxNumberOfChecks);
                    });

            var finalStatus = await retryPolicy.ExecuteAsync(async () =>
            {
                return await GetStatus(responseContent);
            });

            if (finalStatus == WorkFlowStatus.Completed)
            {
                _logger.LogWarning("durable function completed", finalStatus);
                return true;
            }
            else
            {
                _logger.LogWarning("check limit reached", finalStatus);
                return false;
            }
        }
        catch (Exception ex)
        {
            // we want to do this as we don't want to lose records 
            _logger.LogWarning(ex, "An error occurred: {Message} still sending records to queue", ex.Message);
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
}
