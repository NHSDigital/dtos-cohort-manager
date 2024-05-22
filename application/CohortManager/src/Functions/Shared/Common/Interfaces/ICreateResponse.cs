namespace Common;

using System.Runtime.InteropServices;
using System.Net;
using Microsoft.Azure.Functions.Worker.Http;

public interface ICreateResponse
{
    public HttpResponseData CreateHttpResponse(HttpStatusCode statusCode, HttpRequestData httpRequestData);
}
