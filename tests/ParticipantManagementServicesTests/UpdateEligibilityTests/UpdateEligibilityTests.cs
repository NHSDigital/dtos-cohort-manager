namespace NHS.CohortManager.Tests.ParticipantManagementServiceTests;

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
using NHS.CohortManager.Tests.TestUtils;

[TestClass]
public class UpdateEligibilityTests
{
    private readonly Mock<ILogger<UpdateEligibility>> _mockLogger = new();
    private readonly Mock<ICreateResponse> _createResponse = new();
    private readonly Mock<ICallFunction> _callFunction = new();
    private readonly Mock<HttpWebResponse> _webResponse = new();
    private readonly SetupRequest _setupRequest = new();
    private readonly Participant _participant;
    private Mock<HttpRequestData> _request;

    public UpdateEligibilityTests()
    {
        Environment.SetEnvironmentVariable("markParticipantAsEligible", "markParticipantAsEligible");

        _participant = new Participant()
        {
            FirstName = "Joe",
            Surname = "Bloggs",
            NHSId = "1",
            RecordType = Actions.Amended
        };
    }

    [TestMethod]
    public async Task Run_UpdateEligibility_ValidRequest_ReturnsSuccess()
    {
        var json = JsonSerializer.Serialize(_participant);
        var sut = new UpdateEligibility(_mockLogger.Object, _createResponse.Object, _callFunction.Object);

        _request = _setupRequest.Setup(json);

        _createResponse.Setup(x => x.CreateHttpResponse(It.IsAny<HttpStatusCode>(), It.IsAny<HttpRequestData>(), ""))
            .Returns((HttpStatusCode statusCode, HttpRequestData req, string ResponseBody) =>
            {
                var response = req.CreateResponse(statusCode);
                response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
                return response;
            });

        _webResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.OK);

        _callFunction.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("markParticipantAsEligible")), It.IsAny<string>()))
                .Returns(Task.FromResult(_webResponse.Object));


        var result = await sut.Run(_request.Object);

        _createResponse.Verify(response => response.CreateHttpResponse(HttpStatusCode.OK, It.IsAny<HttpRequestData>(), ""), Times.Once);
        _createResponse.VerifyNoOtherCalls();

        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_UpdateEligibility_InvalidRequest_ReturnsBadRequest()
    {
        var json = JsonSerializer.Serialize(_participant);
        var sut = new UpdateEligibility(_mockLogger.Object, _createResponse.Object, _callFunction.Object);

        _request = _setupRequest.Setup(json);

        _createResponse.Setup(x => x.CreateHttpResponse(It.IsAny<HttpStatusCode>(), It.IsAny<HttpRequestData>(), ""))
            .Returns((HttpStatusCode statusCode, HttpRequestData req, string ResponseBody) =>
            {
                var response = req.CreateResponse(statusCode);
                response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
                return response;
            });

        _webResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.BadRequest);

        _callFunction.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("markParticipantAsEligible")), It.IsAny<string>()))
                .Returns(Task.FromResult(_webResponse.Object));


        var result = await sut.Run(_request.Object);

        _createResponse.Verify(response => response.CreateHttpResponse(HttpStatusCode.BadRequest, It.IsAny<HttpRequestData>(), ""), Times.Once);
        _createResponse.VerifyNoOtherCalls();

        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);

    }
}
