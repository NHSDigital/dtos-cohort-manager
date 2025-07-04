namespace NHS.CohortManager.Tests.TestUtils;

using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Options;
using Moq;
using System.Text;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Azure.Core.Serialization;
using System.Net;
using Common;
using System.Collections.Specialized;
using System.Text.Json;

public static class MockHelpers
{
    public static HttpRequestData CreateMockHttpRequestData(string body, string? schema = null)
    {
        var functionContext = new Mock<FunctionContext>();
        var requestData = new Mock<HttpRequestData>(functionContext.Object);
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(Options.Create(new WorkerOptions { Serializer = new JsonObjectSerializer() }));

        var serviceProvider = serviceCollection.BuildServiceProvider();
        functionContext.Setup(context => context.InstanceServices).Returns(serviceProvider);

        var bodyForHttpRequest = GetBodyForHttpRequest(body);
        requestData.Setup(context => context.Body).Returns(bodyForHttpRequest);

        var headersForHttpRequestData = new HttpHeadersCollection();
        if (!string.IsNullOrWhiteSpace(schema))
        {
            headersForHttpRequestData.Add("Authorization", $"{schema} edd2545es.ez5ez5454e.ezdsdsds");
        }
        requestData.Setup(context => context.Headers).Returns(headersForHttpRequestData);

        return requestData.Object;
    }

    public static WebException CreateMockWebException(HttpStatusCode httpStatusCode, string ErrorMessage = "Exception")
    {
        Mock<HttpWebResponse> mockWebResponse = new();
        mockWebResponse.Setup(i => i.StatusCode).Returns(httpStatusCode);
        var webException = new WebException(ErrorMessage, null, WebExceptionStatus.UnknownError, mockWebResponse.Object);
        return webException;
    }

    public static HttpRequestData CreateMockHttpRequestDataWithMethod(string? body, string method, NameValueCollection headers)
    {
        var context = new Mock<FunctionContext>();
        var requestData = new Mock<HttpRequestData>(context.Object);

        if (!string.IsNullOrEmpty(body))
        {
            var bodyForHttpRequest = GetBodyForHttpRequest(body);
            requestData.Setup(context => context.Body).Returns(bodyForHttpRequest);
        }


        requestData.Setup(i => i.Method).Returns(method);
        requestData.Setup(i => i.Query).Returns(headers);


        return requestData.Object;
    }

    public static async Task<TEntity> GetResponseBodyAsObject<TEntity>(HttpResponseData httpResponseData)
    {
        using (var reader = new StreamReader(httpResponseData.Body))
        {
            var result = await reader.ReadToEndAsync();
            return JsonSerializer.Deserialize<TEntity>(result);
        }

    }

    public static HttpWebResponse CreateMockHttpResponseData(HttpStatusCode statusCode, string body = null)
    {
        Mock<HttpWebResponse> httpWebResponse = new();
        httpWebResponse.Setup(x => x.StatusCode).Returns(statusCode);
        if (body != null)
        {
            httpWebResponse.Setup(x => x.GetResponseStream()).Returns(GenerateStreamFromString(body));
        }
        return httpWebResponse.Object;
    }

    private static MemoryStream GetBodyForHttpRequest(string body)
    {
        var byteArray = Encoding.UTF8.GetBytes(body);
        var memoryStream = new MemoryStream(byteArray);
        memoryStream.Flush();
        memoryStream.Position = 0;

        return memoryStream;
    }

    private static MemoryStream GenerateStreamFromString(string s)
    {
        var stream = new MemoryStream();
        var writer = new StreamWriter(stream);
        writer.Write(s);
        writer.Flush();
        stream.Position = 0;
        return stream;
    }

}
