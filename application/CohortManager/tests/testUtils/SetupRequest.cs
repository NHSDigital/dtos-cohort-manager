namespace NHS.CohortManager.Tests.TestUtils;

using System.Text;
using Moq;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker;

public class SetupRequest {
    public Mock<HttpRequestData> request;
    public Mock<FunctionContext> context;
    public SetupRequest() { 
        context = new Mock<FunctionContext>();
        request = new Mock<HttpRequestData>(context.Object);
    }
    public Mock<HttpRequestData> Setup(string json) {
        var byteArray = Encoding.ASCII.GetBytes(json);
        var bodyStream = new MemoryStream(byteArray);

        request.Setup(r => r.Body).Returns(bodyStream);
        request.Setup(r => r.CreateResponse()).Returns(() =>
        {
            var response = new Mock<HttpResponseData>(context.Object);
            response.SetupProperty(r => r.Headers, new HttpHeadersCollection());
            response.SetupProperty(r => r.StatusCode);
            response.SetupProperty(r => r.Body, new MemoryStream());
            return response.Object;
        });

        return request;
    }
}