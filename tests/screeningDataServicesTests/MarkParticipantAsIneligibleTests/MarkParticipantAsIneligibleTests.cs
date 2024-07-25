namespace NHS.CohortManager.Tests.ScreeningDataServicesTests;

using System.Net;
using System.Text;
using System.Text.Json;
using Common;
using Data.Database;
using NHS.CohortManager.ScreeningDataServices;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Model;
using Moq;

[TestClass]
public class MarkParticipantAsIneligibleTests
{
    private readonly Mock<FunctionContext> _context = new();
    private readonly Mock<HttpRequestData> _request;
    private readonly ParticipantCsvRecord _requestBody;
    private readonly MarkParticipantAsIneligible _function;
    private readonly Mock<ICreateResponse> _createResponse = new();
    private readonly Mock<ILogger<MarkParticipantAsIneligible>> _mockLogger = new();
    private readonly Mock<IUpdateParticipantData> _mockUpdateParticipantData = new();
    private readonly Mock<ICallFunction> _callFunction = new();
    private readonly Mock<HttpWebResponse> _webResponse = new();

    private readonly Mock<IExceptionHandler> _handleException = new();

    public MarkParticipantAsIneligibleTests()
    {
        Environment.SetEnvironmentVariable("LookupValidationURL", "LookupValidationURL");

        _request = new Mock<HttpRequestData>(_context.Object);

        _requestBody = new ParticipantCsvRecord
        {
            FileName = "test.csv",
            Participant = new Participant()
            {
                NhsNumber = "1",
            }
        };

        _function = new MarkParticipantAsIneligible(_mockLogger.Object, _createResponse.Object, _mockUpdateParticipantData.Object, _callFunction.Object, _handleException.Object);

        _mockUpdateParticipantData.Setup(x => x.GetParticipant(It.IsAny<string>())).Returns(new Participant());

        _request.Setup(r => r.CreateResponse()).Returns(() =>
            {
                var response = new Mock<HttpResponseData>(_context.Object);
                response.SetupProperty(r => r.Headers, new HttpHeadersCollection());
                response.SetupProperty(r => r.StatusCode);
                response.SetupProperty(r => r.Body, new MemoryStream());
                return response.Object;
            });

        _createResponse.Setup(x => x.CreateHttpResponse(It.IsAny<HttpStatusCode>(), It.IsAny<HttpRequestData>(), It.IsAny<string>()))
            .Returns((HttpStatusCode statusCode, HttpRequestData req, string ResponseBody) =>
            {
                var response = req.CreateResponse(statusCode);
                response.Headers.Add("Content-Type", "application/json; charset=utf-8");
                response.WriteString(ResponseBody);
                return response;
            });
    }

    [TestMethod]
    public async Task Run_Should_Return_BadRequest_When_Request_Body_Empty()
    {
        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_Should_Return_BadRequest_When_Request_Body_Invalid()
    {
        // Arrange
        SetUpRequestBody("Invalid request body");

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_Should_Return_OK_When_Request_Is_Successful()
    {
        // Arrange
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        _webResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.OK);
        _callFunction.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("LookupValidationURL")), It.IsAny<string>()))
            .Returns(Task.FromResult<HttpWebResponse>(_webResponse.Object));

        _mockUpdateParticipantData.Setup(x => x.UpdateParticipantAsEligible(It.IsAny<Participant>(), It.IsAny<char>())).Returns(true);

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_Should_Return_BadRequest_When_Lookup_Validation_Fails()
    {
        // Arrange
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        _webResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.BadRequest);
        _callFunction.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("LookupValidationURL")), It.IsAny<string>()))
            .Returns(Task.FromResult<HttpWebResponse>(_webResponse.Object));

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_Should_Return_BadRequest_When_UpdateParticipantAsEligible_Fails()
    {
        // Arrange
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        _webResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.OK);
        _callFunction.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("LookupValidationURL")), It.IsAny<string>()))
            .Returns(Task.FromResult<HttpWebResponse>(_webResponse.Object));

        _mockUpdateParticipantData.Setup(x => x.UpdateParticipantAsEligible(It.IsAny<Participant>(), It.IsAny<char>())).Returns(false);

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
    }

    private void SetUpRequestBody(string json)
    {
        var byteArray = Encoding.ASCII.GetBytes(json);
        var bodyStream = new MemoryStream(byteArray);

        _request.Setup(r => r.Body).Returns(bodyStream);
    }
}
