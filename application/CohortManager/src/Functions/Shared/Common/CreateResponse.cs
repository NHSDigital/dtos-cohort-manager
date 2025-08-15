namespace Common;

using System.Net;
using System.Text;
using Microsoft.Azure.Functions.Worker.Http;
using Newtonsoft.Json;

public class CreateResponse : ICreateResponse
{
    public HttpResponseData CreateHttpResponse(HttpStatusCode statusCode, HttpRequestData httpRequestData, string responseBody = "")
    {
        var response = httpRequestData.CreateResponse(statusCode);
        response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

        byte[] data = Encoding.UTF8.GetBytes(responseBody);
        response.Body = new MemoryStream(data);

        return response;
    }

    /// <summary>
    /// Asynchronously creates a HTTP response with a body.
    /// </summary>
    public async Task<HttpResponseData> CreateHttpResponseWithBodyAsync(HttpStatusCode statusCode, HttpRequestData requestData, string responseBody)
    {
        var response = requestData.CreateResponse(statusCode);
        response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

        await response.WriteStringAsync(responseBody);
        return response;
    }

    public HttpResponseData CreateHttpResponseWithHeaders(HttpStatusCode statusCode, HttpRequestData httpRequestData, string responseBody, Dictionary<string, string> headers)
    {
        var response = httpRequestData.CreateResponse(statusCode);
        response.Headers.Add("Content-Type", "application/json; charset=utf-8");

        byte[] data = Encoding.UTF8.GetBytes(responseBody);
        response.Body = new MemoryStream(data);

        foreach (var header in headers)
        {
            response.Headers.Add(header.Key, header.Value);
        }

        return response;
    }
}

