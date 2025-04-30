using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System;

namespace TestUtils
{
    public class MockNEMSHttpResponseData : HttpResponseData
    {
        private readonly MemoryStream _body = new();
        private readonly HttpCookies _cookies = new MockNEMSHttpCookies();

        public MockNEMSHttpResponseData(FunctionContext functionContext, HttpStatusCode statusCode)
            : base(functionContext)
        {
            StatusCode = statusCode;
            Headers = new HttpHeadersCollection();
        }

        public override HttpStatusCode StatusCode { get; set; }

        public override HttpHeadersCollection Headers { get; set; }

        public override Stream Body
        {
            get => _body;
            set => throw new NotImplementedException();
        }

        public override HttpCookies Cookies => _cookies;

        public Task WriteStringAsync(
            string text,
            Encoding? encoding = null,
            CancellationToken cancellationToken = default)
        {
            var bytes = (encoding ?? Encoding.UTF8).GetBytes(text);
            _body.Write(bytes, 0, bytes.Length);
            return Task.CompletedTask;
        }
    }




 public class MockNEMSHttpCookies : HttpCookies
{
    private readonly Dictionary<string, IHttpCookie> _cookies = new();

    // Not override — this is just a test helper implementation
    public void Append(string name, string value, CookieOptions options)
    {
        _cookies[name] = new MockNEMSHttpCookie(name, value);
    }

    // Not override — same reason
    public override void Append(string name, string value)
    {
        _cookies[name] = new MockNEMSHttpCookie(name, value);
    }

    public override void Append(IHttpCookie cookie)
    {
        _cookies[cookie.Name] = cookie;
    }

    public IHttpCookie? Get(string name)
    {
        return _cookies.TryGetValue(name, out var cookie) ? cookie : null;
    }

    public IReadOnlyCollection<IHttpCookie> GetAll()
    {
        return _cookies.Values.ToList().AsReadOnly();
    }

    public override IHttpCookie CreateNew()
    {
        return new MockNEMSHttpCookie("defaultName", "defaultValue");
    }
}

    public class MockNEMSHttpCookie : IHttpCookie
    {
        public MockNEMSHttpCookie(string name, string value)
        {
            Name = name;
            Value = value;
        }

        public string Name { get; }
        public string Value { get; }

        public string? Domain { get; set; }
        public DateTimeOffset? Expires { get; set; }
        public bool? HttpOnly { get; set; } // Fix: Change to bool? to match IHttpCookie
        public double? MaxAge { get; set; }
        public string? Path { get; set; }
        public SameSite SameSite { get; set; }
        public bool? Secure { get; set; } // Fix: Change to bool? to match IHttpCookie
    }
}
