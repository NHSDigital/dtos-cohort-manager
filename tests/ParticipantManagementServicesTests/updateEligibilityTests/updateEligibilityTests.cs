namespace NHS.CohortManager.Tests.ParticipantManagementService;

using Moq;
using Microsoft.Extensions.Logging;
using Common;
using System.Net;
using System.Text.Json;
using Model;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Text;
using NHS.CohortManager.CaasIntegration.UpdateEligibility;

[TestClass]
public class UpdateEligibilityTests
{
    private readonly Mock<ILogger<UpdateEligibility>> _mockLogger = new();
    private readonly Mock<ICreateResponse> _createResponse = new();
    private readonly Mock<ICallFunction> _callFunction = new();
    private readonly Mock<HttpWebResponse> _webResponse = new();
    private readonly Mock<FunctionContext> _functionContext = new();
    private readonly Mock<HttpRequestData> _request;
    private readonly Participant _participant;

    public UpdateEligibilityTests()
    {
        Environment.SetEnvironmentVariable("markParticipantAsEligible", "markParticipantAsEligible");
        _request = new Mock<HttpRequestData>(_functionContext.Object);

        _participant = new Participant()
        {
            FirstName = "Joe",
            Surname = "Bloggs",
            NHSId = "1",
            Action = "UPDATE"
        };
    }

    [TestMethod]
    public async Task Run_UpdateEligibility_ValidRequest_ReturnsSuccess()
    {
        _webResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.OK);
        var json = JsonSerializer.Serialize(_participant);

        _callFunction.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("markParticipantAsEligible")), It.IsAny<string>()))
                .Returns(Task.FromResult(_webResponse.Object));

        SetupRequest(json);
        var sut = new UpdateEligibility(_mockLogger.Object, _createResponse.Object, _callFunction.Object);

        await sut.Run(_request.Object);

        _createResponse.Verify(response => response.CreateHttpResponse(HttpStatusCode.OK, It.IsAny<HttpRequestData>()), Times.Once);
        _createResponse.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task Run_UpdateEligibility_InvalidRequest_ReturnsBadRequest()
    {
        _webResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.BadRequest);
        var json = JsonSerializer.Serialize(_participant);

        _callFunction.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("markParticipantAsEligible")), It.IsAny<string>()))
                .Returns(Task.FromResult(_webResponse.Object));

        SetupRequest(json);
        var sut = new UpdateEligibility(_mockLogger.Object, _createResponse.Object, _callFunction.Object);

        await sut.Run(_request.Object);

        _createResponse.Verify(response => response.CreateHttpResponse(HttpStatusCode.BadRequest, It.IsAny<HttpRequestData>()), Times.Once);
        _createResponse.VerifyNoOtherCalls();

    }

    private void SetupRequest(string json)
    {
        var byteArray = Encoding.ASCII.GetBytes(json);
        var bodyStream = new MemoryStream(byteArray);

        _request.Setup(r => r.Body).Returns(bodyStream);
        _request.Setup(r => r.CreateResponse()).Returns(() =>
        {
            var response = new Mock<HttpResponseData>(_functionContext.Object);
            response.SetupProperty(r => r.Headers, new HttpHeadersCollection());
            response.SetupProperty(r => r.StatusCode);
            response.SetupProperty(r => r.Body, new MemoryStream());
            return response.Object;
        });

    }
}
