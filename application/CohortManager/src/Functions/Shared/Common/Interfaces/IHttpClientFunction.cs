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
    /// <returns>string<returns>
    Task<string> SendGet(string url);

    /// <summary>
    /// Performs a GET request using HttpClient.
    /// </summary>
    /// <param name="url">URL to be used in request.</param>
    /// <param name="parameters">Parameters to be added to the url and used in request.</param>
    /// <returns>string<returns>
    Task<string> SendGet(string url, Dictionary<string, string> parameters);

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
    Task<HttpResponseMessage> SendPdsGet(string url);

    /// <summary>
    /// Sends an HTTP POST request to the specified NEMS URL with the provided subscription JSON, headers, and authorization tokens.
    /// </summary>
    /// <param name="subscriptionJson">The body of the request in JSON format. This represents the subscription details to be sent.</param>
    /// <param name="spineAccessToken">The authorization token to be included in the request headers for Bearer authentication.</param>
    /// <param name="fromAsid">The ASID (Application Service Identifier) of the sender, used for the request headers.</param>
    /// <param name="toAsid">The ASID (Application Service Identifier) of the receiver, used for the request headers.</param>
    /// <param name="url">URL to be used in request.</param>
    /// <remarks>
    /// This method sends a POST request to the specified NEMS endpoint with a JSON body and the necessary authorization headers.
    /// The request headers include:
    /// - <c>Authorization</c> with a Bearer token,
    /// - <c>fromASID</c> and <c>toASID</c> to specify the sender and receiver ASID values,
    /// - <c>Interaction-ID</c> to specify the interaction ID of the subscription creation process.
    /// This is a WIP as additional work is required to use the NEMS endpoint after onboarding to NemsApi hub. Currently it's just a basic structure.
    /// </remarks>
    Task<HttpResponseMessage> SendNemsPost(string url, string subscriptionJson, string spineAccessToken, string fromAsid, string toAsid);

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
    /// <returns>bool<returns>
    Task<bool> SendDelete(string url);

    /// <summary>
    /// Reads HTTP response content and returns it as a string.
    /// </summary>
    /// <param name="response">HTTP response message.</param>
    /// <returns>string<returns>
    Task<string> GetResponseText(HttpResponseMessage response);
}
