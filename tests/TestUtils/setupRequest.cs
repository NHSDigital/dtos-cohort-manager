namespace NHS.CohortManager.Tests.TestUtils;

using System.Text;
using Moq;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker;
using System.Collections.Specialized;

public class SetupRequest
{
    private readonly Mock<HttpRequestData> _request;
    private readonly Mock<FunctionContext> _context;

    public SetupRequest()
    {
        _context = new Mock<FunctionContext>();
        _request = new Mock<HttpRequestData>(_context.Object);
    }

    public Mock<HttpRequestData> Setup(string? json = null)
    {
        if (json == null)
        {
            _request.Setup(r => r.Body).Returns((MemoryStream)null);
        }
        else
        {
            var byteArray = Encoding.ASCII.GetBytes(json);
            var bodyStream = new MemoryStream(byteArray);

            _request.Setup(r => r.Body).Returns(bodyStream);
        }

        _request.Setup(r => r.CreateResponse()).Returns(() =>
        {
            var response = new Mock<HttpResponseData>(_context.Object);
            response.SetupProperty(r => r.Headers, new HttpHeadersCollection());
            response.SetupProperty(r => r.StatusCode);
            response.SetupProperty(r => r.Body, new MemoryStream());
            return response.Object;
        });

        return _request;
    }

    public Mock<HttpRequestData> Setup(string? json = null, NameValueCollection urlQueries = null)
    {
        if (json == null)
        {
            _request.Setup(r => r.Body).Returns((MemoryStream)null);
        }
        else
        {
            var byteArray = Encoding.ASCII.GetBytes(json);
            var bodyStream = new MemoryStream(byteArray);

            _request.Setup(r => r.Body).Returns(bodyStream);
        }
        if (urlQueries != null)
        {
            _request.Setup(r => r.Query).Returns(urlQueries);
        }

        _request.Setup(r => r.CreateResponse()).Returns(() =>
        {
            var response = new Mock<HttpResponseData>(_context.Object);
            response.SetupProperty(r => r.Headers, new HttpHeadersCollection());
            response.SetupProperty(r => r.StatusCode);
            response.SetupProperty(r => r.Body, new MemoryStream());

            return response.Object;
        });

        return _request;

    }
}
