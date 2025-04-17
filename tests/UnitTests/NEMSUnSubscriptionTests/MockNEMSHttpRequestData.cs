
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker;
using System.IO;
using System.Net;
using System.Security.Claims;

public class MockNEMSHttpRequestData : HttpRequestData
{
    private readonly HttpResponseData _response;

    public MockNEMSHttpRequestData(FunctionContext functionContext, Stream body, HttpResponseData response)
        : base(functionContext)
    {
        Body = body;
        _response = response;
    }

    public override Stream Body { get; }

    public override HttpHeadersCollection Headers => new();

    public override IReadOnlyCollection<IHttpCookie> Cookies => Array.Empty<IHttpCookie>();

    public override Uri Url => new("http://localhost");

    public override IEnumerable<ClaimsIdentity> Identities => Enumerable.Empty<ClaimsIdentity>();

    public override string Method => "POST";

    public override HttpResponseData CreateResponse() => _response;

    // ❌ Removed CreateResponse(HttpStatusCode)
}
