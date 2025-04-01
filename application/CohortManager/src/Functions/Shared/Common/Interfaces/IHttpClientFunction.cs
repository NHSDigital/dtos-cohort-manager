namespace Common;

public interface IHttpClientFunction
{
    Task<HttpResponseMessage> GetAsync(string url, Dictionary<string, string> headers);
}
