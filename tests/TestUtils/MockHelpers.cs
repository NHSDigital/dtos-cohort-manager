namespace screeningDataServices;

using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Options;
using Moq;
using System.Text;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Azure.Core.Serialization;

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

    private static MemoryStream GetBodyForHttpRequest(string body)
    {
        var byteArray = Encoding.UTF8.GetBytes(body);
        var memoryStream = new MemoryStream(byteArray);
        memoryStream.Flush();
        memoryStream.Position = 0;

        return memoryStream;
    }
}
