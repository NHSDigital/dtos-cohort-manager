namespace Common;

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Model;
using Model.Enums;
using Polly;
using Polly.Retry;

public class CheckDemographic : ICheckDemographic
{
    private readonly ICallFunction _callFunction;
    private readonly ILogger<ICheckDemographic> _logger;

    private readonly HttpClient _httpClient = new HttpClient();


    public CheckDemographic(ICallFunction callFunction, ILogger<ICheckDemographic> logger, HttpClient httpClient)
    {
        _callFunction = callFunction;
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task<Demographic> GetDemographicAsync(string NhsNumber, string DemographicFunctionURI)
    {
        var url = $"{DemographicFunctionURI}?Id={NhsNumber}";

        var response = await _callFunction.SendGet(url);
        var demographicData = JsonSerializer.Deserialize<Demographic>(response);

        return demographicData;
    }

    public async Task<bool> PostDemographicDataAsync(List<ParticipantDemographic> participants, string DemographicFunctionURI)
    {
        if(participants.Count == 0) 
        {
            _logger.LogInformation("There were no items to to send to the demographic durable function");
            return false;
        }
        using var memoryStream = new MemoryStream();
        // this seems to be better for memory management 
        await JsonSerializer.SerializeAsync(memoryStream, participants);
        memoryStream.Position = 0;

        var content = new StreamContent(memoryStream);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        var response = await _httpClient.PostAsync(DemographicFunctionURI, content);

        var responseContent = response.Headers.Location.ToString();

        var maxNumberOfChecks = 20;
        var delayBetweenChecks = TimeSpan.FromSeconds(10);

        // this is not retrying the function if it fails but checking if it has done yet. 
        var retryPolicy = Policy
            .HandleResult<WorkFlowStatus>(status => status != WorkFlowStatus.Completed)
            .WaitAndRetryAsync(maxNumberOfChecks, check => delayBetweenChecks,
                (result, timeSpan, checkCount, context) =>
                {
                    _logger.LogWarning($"Status: {result.Result}, checking status: ({checkCount}/{maxNumberOfChecks})...");
                });
        try
        {
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
            _logger.LogWarning($"An error occurred: {ex.Message}");
            return false;
        }
    }




    private async Task<WorkFlowStatus> GetStatus(string statusRequestGetUri)
    {
        using HttpResponseMessage response = await _httpClient.GetAsync(statusRequestGetUri);
        var jsonResponse = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<RuntimeStatus>(jsonResponse);

        if (data != null)
        {
            Enum.TryParse(data.runtimeStatus, out WorkFlowStatus workFlowStatus);
            return workFlowStatus;
        }
        return WorkFlowStatus.Unknown;
    }
}
