namespace Common;

using System.Text;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker.Http;
using Model;

public class GetParticipantData : IGetParticipantData
{
    public async Task<Participant> GetParticipantDetails(HttpRequestData req)
    {
        // parse through the HTTP request
        string requestBody = "";
        var participantData = new Participant();
        using (StreamReader reader = new StreamReader(req.Body, Encoding.UTF8))
        {
            requestBody = await reader.ReadToEndAsync();
            participantData = JsonSerializer.Deserialize<Participant>(requestBody);
        }

        return participantData;
    }
}
