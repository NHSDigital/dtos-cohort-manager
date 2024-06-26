namespace Common;

using System.Text;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker.Http;
using Model;

public class GetParticipantData : IGetParticipantData
{
    private readonly ICallFunction _callFunction;

    public GetParticipantData(ICallFunction callFunction)
    {
        _callFunction = callFunction;
    }
    public async Task<Participant> GetParticipantDetails(HttpRequestData req)
    {
        var participantData = new Participant();
        using (StreamReader reader = new StreamReader(req.Body, Encoding.UTF8))
        {
            string requestBody = await reader.ReadToEndAsync();
            participantData = JsonSerializer.Deserialize<Participant>(requestBody);
        }

        return participantData;
    }
    public async Task<Participant> GetParticipantAsync(string NHSId, string ParticipantFunctionURI)
    {
        var url = $"{ParticipantFunctionURI}?Id={NHSId}";

        var response = await _callFunction.SendGet(url);
        var participantData = JsonSerializer.Deserialize<Participant>(response);

        return participantData;
    }
}
