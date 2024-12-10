namespace Common;

using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Model;
using Model.Enums;

public class CheckDemographic : ICheckDemographic
{
    private readonly ICallFunction _callFunction;
    private readonly ILogger<ICheckDemographic> _logger;

    public CheckDemographic(ICallFunction callFunction, ILogger<ICheckDemographic> logger)
    {
        _callFunction = callFunction;
        _logger = logger;
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
        var json = JsonSerializer.Serialize(participants);
        using var client = new HttpClient();

        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await client.PostAsync(DemographicFunctionURI, content);

        var responseContent = response.Headers.Location.ToString();
        var checkTimerSeconds = Environment.GetEnvironmentVariable("checkTimerSeconds") ?? "800";


        try
        {
            if (responseContent != null)
            {
                var responseStatus = await GetStatus(responseContent, client);
                _logger.LogInformation("Demographic function status: {responseStatus}", responseStatus);

                var cancellationsToken = new CancellationTokenSource();

                await CallDemographicFunction(async () =>
                {
                    responseStatus = await GetStatus(responseContent, client);
                    if (responseStatus == WorkflowStatus.Failed)
                    {
                        return;
                    }
                    _logger.LogWarning(responseStatus.ToString());
                },
                () => responseStatus == WorkflowStatus.Completed,
                cancellationsToken.Token,
                int.Parse(checkTimerSeconds));

                _logger.LogInformation("Demographic function status: {responseStatus} for number of records: {number}", responseStatus, participants.Count);
                return true;
            }
            _logger.LogError("The response content back from demographic was was null");
            return false;

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "There was an error while sending Demographic data to the durable function");
            return false;
        }
        finally
        {
            client.Dispose();
        }
    }

    private async Task CallDemographicFunction(
       Action getStatus,
       Func<bool> condition,
       CancellationToken cancellationToken,
       int checkTimerSeconds)
    {
        while (!condition() && !cancellationToken.IsCancellationRequested)
        {
            getStatus();
            try
            {
                await Task.Delay(checkTimerSeconds, cancellationToken);
            }
            catch (TaskCanceledException)
            {
                _logger.LogError("There was a problem while canceling CallDemographicFunction");
                break;
            }
        }
    }


    private async Task<WorkflowStatus> GetStatus(string statusRequestGetUri, HttpClient client)
    {
        using HttpResponseMessage response = await client.GetAsync(statusRequestGetUri);
        var jsonResponse = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<RuntimeStatus>(jsonResponse);

        if (data != null)
        {
            Enum.TryParse(data.runtimeStatus, out WorkflowStatus workFlowStatus);
            return workFlowStatus;
        }
        return WorkflowStatus.Unknown;
    }
}
