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
    /// Performs a GET request using HttpClient and returns the response body as a string or null if the request failed.
    /// </summary>
    /// <param name="url">URL to be used in request.</param>
    /// <returns>string</returns>
    Task<string> SendGet(string url);

    /// <summary>
    /// Performs a GET request using HttpClient and returns the response body as a string or null if the request failed.
    /// </summary>
    /// <param name="url">URL to be used in request.</param>
    /// <param name="parameters">Parameters to be added to the url and used in request.</param>
    /// <returns>string</returns>
    Task<string> SendGet(string url, Dictionary<string, string> parameters);

    Task<HttpResponseMessage> SendGetHttpResponse(string url, Dictionary<string, string> parameters);

    /// <summary>
    /// Performs a GET request using HttpClient and returns the entire HTTP response.
    /// </summary>
    /// <param name="url">URL to be used in request.</param>
    /// <returns>HttpResponseMessage</returns>
    Task<HttpResponseMessage> SendGetResponse(string url);

    /// <summary>
    /// Sends a get request or throws an error
    /// </summary>
    /// <param name="url"></param>
    /// <returns>string representing the serialised response body, string.Empty if it returns nothing </returns>
    /// <exception cref="HttpRequestException"></exception>
    /// <exception cref="TaskCanceledException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    Task<string> SendGetOrThrowAsync(string url);

    /// <summary>
    /// Performs a GET request to a PDS endpoint using HttpClient.
    /// This is a WIP as additional work is required to use the PDS endpoint from cohort manager. Currently it just uses the PDS sandbox API.
    /// </summary>
    /// <param name="url">URL to be used in request.</param>
    /// <returns>HttpResponseMessage<returns>
    Task<HttpResponseMessage> SendPdsGet(string url, string bearerToken);

    /// <summary>
    /// Performs a PUT request using HttpClient.
    /// </summary>
    /// <param name="url">URL to be used in request.</param>
    /// <param name="data">Data to be sent in request.</param>
    /// <returns>HttpResponseMessage</returns>
    Task<HttpResponseMessage> SendPut(string url, string data);

    /// <summary>
    /// Performs a DELETE request using HttpClient.
    /// </summary>
    /// <param name="url">URL to be used in request.</param>
    /// <returns>bool</returns>
    Task<bool> SendDelete(string url);

    /// <summary>
    /// Reads HTTP response content and returns it as a string.
    /// </summary>
    /// <param name="response">HTTP response message.</param>
    /// <returns>string<returns>
    Task<string> GetResponseText(HttpResponseMessage response);
}
