namespace Common;

using System.Net;
using System.Text;
using Microsoft.Azure.Functions.Worker.Http;
using Newtonsoft.Json;

public class CreateResponse : ICreateResponse
{
    /// <summary>
    /// Creates a HTTP response.
    /// NOTE: adding a response body does not work, use CreateHttpResponseWithBodyAsync if you need to return a body.
    /// </summary>
    public HttpResponseData CreateHttpResponse(HttpStatusCode statusCode, HttpRequestData requestData, string responseBody = "")
    {
        var response = requestData.CreateResponse(statusCode);
        response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

        byte[] data = Encoding.UTF8.GetBytes(responseBody);
        response.Body = new MemoryStream(data);

        return response;
    }

    /// <summary>
    /// Asyncronously creates a HTTP response with a body.
    /// </summary>
    public async Task<HttpResponseData> CreateHttpResponseWithBodyAsync(HttpStatusCode statusCode, HttpRequestData requestData, string responseBody) {
        var response = requestData.CreateResponse(statusCode);
        response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

        var json = JsonConvert.SerializeObject(responseBody);
        await response.WriteStringAsync(json);
        return response;
    }
}
