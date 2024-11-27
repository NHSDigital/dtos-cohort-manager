using System.Collections.Specialized;
using System.Security.Claims;
using System.Text;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

public sealed class MockHttpRequestData : HttpRequestData
{
    private readonly FunctionContext context;

    public MockHttpRequestData(
        FunctionContext context,
        string body,
        string method)
            : base(context)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(body);
        Body = new MemoryStream(bytes);
        this.context = context;
        Method = method;
        Cookies = new List<IHttpCookie>().AsReadOnly();

        Identities = new List<ClaimsIdentity>();
    }

    public override Stream Body { get; }

    public override HttpHeadersCollection Headers { get; } = [];

    public override IReadOnlyCollection<IHttpCookie> Cookies { get; }

    public override Uri Url { get; }

    public override NameValueCollection Query {get;} = new();

    public override IEnumerable<ClaimsIdentity> Identities { get; }

    public override string Method { get; }

    public override HttpResponseData CreateResponse()
    {
        MockHttpResponseData response = new(this.context);

        return response;
    }

    public void AddHeaderKeyVal(string key, string value)
    {
        Headers.Add(key, value);
    }

    public void AddQuery(string key, string value)
    {
        Query.Add(key, value);
    }
}
