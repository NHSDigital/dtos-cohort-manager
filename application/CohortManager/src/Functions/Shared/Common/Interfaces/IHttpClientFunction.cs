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
    /// <summary>
    /// Performs a POST request using HttpClient.
    /// </summary>
    /// <param name="url">URL to be used in request.</param>
    /// <param name="postData">Data to be sent in request.</param>
    /// <returns>HttpResponseMessage<returns>
    Task<HttpResponseMessage> PostAsync(string url, string postData);
}
