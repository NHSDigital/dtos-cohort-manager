namespace Common;

public interface IHttpClientFunction
{
    /// <summary>
    /// Performs a GET request using HttpClient.
    /// </summary>
    /// <param name="url">URL to be used in request.</param>
    /// <param name="headers">Headers to be used in request.</param>
    /// <returns>HttpResponseMessage<returns>
    Task<HttpResponseMessage> GetAsync(string url, Dictionary<string, string> headers);
}
