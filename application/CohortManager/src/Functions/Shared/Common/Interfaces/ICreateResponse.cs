namespace Common;

using System.Net;
using Microsoft.Azure.Functions.Worker.Http;

public interface ICreateResponse
{
    HttpResponseData CreateHttpResponse(HttpStatusCode statusCode, HttpRequestData httpRequestData, string responseBody = "");
    Task<HttpResponseData> CreateHttpResponseWithBodyAsync(HttpStatusCode statusCode, HttpRequestData requestData, string responseBody);
    HttpResponseData CreateHttpResponseWithHeaders(HttpStatusCode statusCode, HttpRequestData httpRequestData, string responseBody, Dictionary<string, string> headers);
}
