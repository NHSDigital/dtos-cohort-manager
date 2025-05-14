namespace Common;

public interface IServiceNowClient
{
    Task<HttpResponseMessage> SendServiceNowMessageAsync(string caseId, object payload);
}

