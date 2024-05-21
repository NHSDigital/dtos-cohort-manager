using System.Net;
using Common;
using Grpc.Core;
using Microsoft.Azure.Functions.Worker.Http;

namespace Common;

public class CreateResponse : ICreateResponse
{
    public HttpResponseData CreateHttpResponse(HttpStatusCode statusCode, HttpRequestData req)
    {
        var response = req.CreateResponse(statusCode);
        response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
        return response;
    }
}
