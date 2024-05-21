using Microsoft.Azure.Functions.Worker.Http;
using Model;

namespace Common;

public interface IGetParticipantData
{
    public Task<Participant> GetParticipantDetails(HttpRequestData req);
}
