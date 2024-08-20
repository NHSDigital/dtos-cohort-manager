
using System.Net;
using System.Text;
using System.Text.Json;
using Common;
using Common.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Model;
using Moq;
using NHS.CohortManager.CohortDistribution.ValidateCohortDistributionRecord;

namespace ValidateCohortDistributionRecordTests;

[TestClass]
public class ValidateCohortDistributionRecordTests
{
    private readonly Mock<ILogger<ValidateCohortDistributionRecord>> _logger = new();
    private readonly Mock<ICreateResponse> _createResponse = new();
    private readonly Mock<ICreateCohortDistributionData> _createCohortDistributionData = new();
    private readonly Mock<IExceptionHandler> _exceptionHandler = new();
    private readonly Mock<ICallFunction> _callFunction = new();
    private readonly Mock<ICreateParticipant> _createParticipant = new();

    private readonly ValidateCohortDistributionRecord _function;

    private readonly Mock<HttpWebResponse> _webResponse = new();
    private readonly Mock<FunctionContext> _context = new();
    private readonly Mock<HttpRequestData> _request;
    private readonly ValidateCohortDistributionRecordBody _requestBody;


    public ValidateCohortDistributionRecordTests()
    {
        Environment.SetEnvironmentVariable("LookupValidationURL", "LookupValidationURL");
        Environment.SetEnvironmentVariable("DtOsDatabaseConnectionString", "SomeConnectionString");

        _request = new Mock<HttpRequestData>(_context.Object);

        _requestBody = new ValidateCohortDistributionRecordBody()
        {
            NhsNumber = "1111111",
            FileName = "some_file_name",
            CohortDistributionParticipant = new CohortDistributionParticipant()
        };

        _function = new ValidateCohortDistributionRecord(_logger.Object, _createResponse.Object, _createCohortDistributionData.Object, _exceptionHandler.Object, _callFunction.Object, _createParticipant.Object);

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
        //Arrange 
        _exceptionHandler.Setup(x => x.CreateSystemExceptionLogFromNhsNumber(It.IsAny<Exception>(), It.IsAny<string>(), It.IsAny<string>())).Verifiable();
        //Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        _logger.Verify(
                m => m.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ),
            Times.Once,
            "there was an error while deserializing records"
            );

        _exceptionHandler.Verify(
        x => x.CreateSystemExceptionLogFromNhsNumber(It.IsAny<Exception>(),
        It.IsAny<string>(), It.IsAny<string>()),
        Times.Once);


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

        _logger.Verify(
                m => m.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ),
                Times.Once,
                "there was an error while deserializing records"
            );

        _exceptionHandler.Verify(
            x => x.CreateSystemExceptionLogFromNhsNumber(It.IsAny<Exception>(),
            It.IsAny<string>(), It.IsAny<string>()),
            Times.Once);

        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_Should_Return_Created_When_Lookup_Validation_Creates_Validation_Error()
    {
        // Arrange
        SetUpRequestBody(JsonSerializer.Serialize(_requestBody));
        var existingParticipant = new CohortDistributionParticipant();
        _createCohortDistributionData.Setup(x => x.GetLastCohortDistributionParticipant(It.IsAny<string>()))
            .Returns(existingParticipant);

        _webResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.Created);


        _callFunction.Setup(x => x.SendPost(It.Is<string>(x => x.Contains("LookupValidationURL")), It.IsAny<string>()))
            .Returns(Task.FromResult<HttpWebResponse>(_webResponse.Object));

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.Created, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_Should_Return_OK_When_Lookup_Validation_Does_Not_Create_Validation_error()
    {
        // Arrange
        SetUpRequestBody(JsonSerializer.Serialize(_requestBody));
        var existingParticipant = new CohortDistributionParticipant();
        _createCohortDistributionData.Setup(x => x.GetLastCohortDistributionParticipant(It.IsAny<string>()))
            .Returns(existingParticipant);

        _webResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.OK);

        _callFunction.Setup(x => x.SendPost(It.Is<string>(x => x.Contains("LookupValidationURL")), It.IsAny<string>()))
            .Returns(Task.FromResult<HttpWebResponse>(_webResponse.Object));

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_Should_Return_InternalServerError_When_Exception_Occurs_During_Validation()
    {
        // Arrange
        SetUpRequestBody(JsonSerializer.Serialize(_requestBody));
        var existingParticipant = new CohortDistributionParticipant();
        _createCohortDistributionData.Setup(x => x.GetLastCohortDistributionParticipant(It.IsAny<string>()))
            .Returns(existingParticipant);

        _callFunction.Setup(x => x.SendPost(It.Is<string>(x => x.Contains("LookupValidationURL")), It.IsAny<string>()))
        .Throws(new Exception("some new exception"));

        _exceptionHandler.Setup(x => x.CreateSystemExceptionLogFromNhsNumber(It.IsAny<Exception>(), It.IsAny<string>(), It.IsAny<string>()))
        .Verifiable();

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        _exceptionHandler.Verify(x => x.CreateSystemExceptionLogFromNhsNumber(It.IsAny<Exception>(), _requestBody.NhsNumber, _requestBody.FileName), Times.Once);
        Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_Should_Return_InternalServerError_When_Exception_Occurs_During_Getting_Participant()
    {
        // Arrange
        var exception = new Exception("some new exception");

        SetUpRequestBody(JsonSerializer.Serialize(_requestBody));
        var existingParticipant = new CohortDistributionParticipant();
        _createCohortDistributionData.Setup(x => x.GetLastCohortDistributionParticipant(It.IsAny<string>()))
        .Returns(existingParticipant);

        _createCohortDistributionData.Setup(x => x.GetLastCohortDistributionParticipant(_requestBody.NhsNumber))
        .Throws(exception);

        _exceptionHandler.Setup(x => x.CreateSystemExceptionLogFromNhsNumber(It.IsAny<Exception>(), It.IsAny<string>(), It.IsAny<string>()))
        .Verifiable();

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        _logger.Verify(
                m => m.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ),
            Times.Once,
            $"there was an error validating the cohort distribution records {exception.Message}"
        );
        _exceptionHandler.Verify(x => x.CreateSystemExceptionLogFromNhsNumber(It.IsAny<Exception>(), _requestBody.NhsNumber, _requestBody.FileName), Times.Once);
        Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
    }

    private void SetUpRequestBody(string json)
    {
        var byteArray = Encoding.ASCII.GetBytes(json);
        var bodyStream = new MemoryStream(byteArray);

        _request.Setup(r => r.Body).Returns(bodyStream);
    }
}