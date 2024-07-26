namespace NHS.CohortManager.Tests.RetrieveParticipantDataTests;

using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Moq;
using Common;
using Microsoft.Extensions.Logging;
using NHS.CohortManager.CohortDistributionService;
using NHS.CohortManager.CohortDistribution;
using NHS.CohortManager.Tests.TestUtils;
using Model;
using Data.Database;

[TestClass]
public class RetrieveParticipantDataTests
{
    private readonly Mock<ICallFunction> _callFunction = new();
    private readonly Mock<ICreateResponse> _createResponse = new();
    private readonly Mock<ILogger<RetrieveParticipantData>> _logger = new();
    private readonly RetrieveParticipantData _function;
    private readonly Mock<FunctionContext> _context = new();
    private readonly Mock<HttpRequestData> _request;
    private readonly RetrieveParticipantRequestBody _requestBody;
    private readonly Mock<IUpdateParticipantData> _updateParticipantData = new();
    private readonly Mock<ICreateDemographicData> _createDemographicData = new();

    public RetrieveParticipantDataTests()
    {
        _request = new Mock<HttpRequestData>(_context.Object);

        _requestBody = new RetrieveParticipantRequestBody()
        {
            NhsNumber = "1234567890",
            ScreeningService = "BSS"
        };

        _function = new RetrieveParticipantData(_createResponse.Object, _logger.Object, _callFunction.Object, _updateParticipantData.Object, _createDemographicData.Object);

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
    public async Task Run_Should_Return_BadRequest_When_GetParticipant_Fails()
    {
        // Arrange
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        _updateParticipantData.Setup(x => x.GetParticipant(It.IsAny<string>())).Throws(new Exception("there has been an error"));
        _createDemographicData.Setup(x => x.GetDemographicData(It.IsAny<string>())).Returns(new Demographic());

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_Should_Return_BadRequest_When_GetDemographicData_Fails()
    {
        // Arrange
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        _updateParticipantData.Setup(x => x.GetParticipant(It.IsAny<string>())).Returns(new Participant());
        _createDemographicData.Setup(x => x.GetDemographicData(It.IsAny<string>())).Throws(new Exception("there has been an error"));

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_Should_Return_OK_When_Request_Body_Valid()
    {
        // Arrange
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        _updateParticipantData.Setup(x => x.GetParticipant(It.IsAny<string>())).Returns(new Participant());
        _createDemographicData.Setup(x => x.GetDemographicData(It.IsAny<string>())).Returns(new Demographic());

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
    }

    private void SetUpRequestBody(string json)
    {
        var byteArray = Encoding.ASCII.GetBytes(json);
        var bodyStream = new MemoryStream(byteArray);

        _request.Setup(r => r.Body).Returns(bodyStream);
    }
}
