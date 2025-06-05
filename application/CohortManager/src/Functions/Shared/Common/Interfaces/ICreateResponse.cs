namespace Common;

using System.Net;
using Microsoft.Azure.Functions.Worker.Http;
using Model;

public interface ICreateResponse
{
    public HttpResponseData CreateHttpResponse(HttpStatusCode statusCode, HttpRequestData httpRequestData, string responseBody = "");
   // public HttpResponseData CreateHttpResponse(HttpStatusCode statusCode, HttpRequestData req, string responseBody = "", ErrorResponse? errorResponse = null);
    public Task<HttpResponseData> CreateHttpResponseWithBodyAsync(HttpStatusCode statusCode, HttpRequestData requestData, string responseBody);
}
