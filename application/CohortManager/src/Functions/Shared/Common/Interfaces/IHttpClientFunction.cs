namespace Common;

public interface IHttpClientFunction
{
    /// <summary>
    /// Performs a POST request using HttpClient.
    /// </summary>
    /// <param name="url">URL to be used in request.</param>
    /// <param name="data">Data to be sent in request.</param>
    /// <returns>HttpResponseMessage<returns>
    Task<HttpResponseMessage> SendPost(string url, string data);

    /// <summary>
    /// Performs a GET request using HttpClient.
    /// </summary>
    /// <param name="url">URL to be used in request.</param>
    /// <returns>HttpResponseMessage<returns>
    Task<HttpResponseMessage> SendGet(string url);

    /// <summary>
    /// Performs a GET request using HttpClient.
    /// </summary>
    /// <param name="url">URL to be used in request.</param>
    /// <param name="parameters">Parameters to be added to the url and used in request.</param>
    /// <returns>HttpResponseMessage<returns>
    Task<HttpResponseMessage> SendGet(string url, Dictionary<string, string> parameters);

    /// <summary>
    /// Performs a GET request to a PDS endpoint using HttpClient.
    /// This is a WIP as additional work is required to use the PDS endpoint from cohort manager. Currently it just uses the PDS sandbox API.
    /// </summary>
    /// <param name="url">URL to be used in request.</param>
    /// <returns>HttpResponseMessage<returns>
    Task<HttpResponseMessage> SendPdsGet(string url);

    /// <summary>
    /// Performs a PUT request using HttpClient.
    /// </summary>
    /// <param name="url">URL to be used in request.</param>
    /// <param name="data">Data to be sent in request.</param>
    /// <returns>HttpResponseMessage<returns>
    Task<HttpResponseMessage> SendPut(string url, string data);

    /// <summary>
    /// Performs a DELETE request using HttpClient.
    /// </summary>
    /// <param name="url">URL to be used in request.</param>
    /// <returns>HttpResponseMessage<returns>
    Task<HttpResponseMessage> SendDelete(string url);
}
