namespace Common;

using System.Net;
using Microsoft.Azure.Functions.Worker.Http;

public interface ICreateResponse
{
    public HttpResponseData CreateHttpResponse(HttpStatusCode statusCode, HttpRequestData httpRequestData, string responseBody = "");
    public Task<HttpResponseData> CreateHttpResponseWithBodyAsync(HttpStatusCode statusCode, HttpRequestData requestData, string responseBody);
}
