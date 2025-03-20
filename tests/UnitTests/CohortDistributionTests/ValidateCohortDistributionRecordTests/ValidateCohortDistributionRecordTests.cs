namespace NHS.CohortManager.Tests.UnitTests.ValidateCohortDistributionRecordTests;

using System.Net;
using System.Text;
using System.Text.Json;
using Common;
using Common.Interfaces;
using DataServices.Client;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Model;
using Moq;
using NHS.CohortManager.CohortDistribution.ValidateCohortDistributionRecord;
using NHS.Screening.ValidateCohortDistributionRecord;

[TestClass]
public class ValidateCohortDistributionRecordTests
{
    private readonly Mock<ILogger<ValidateCohortDistributionRecord>> _logger = new();
    private readonly Mock<ICreateResponse> _createResponse = new();
    private readonly Mock<IExceptionHandler> _exceptionHandler = new();
    private readonly Mock<ICallFunction> _callFunction = new();
    private readonly ValidateCohortDistributionRecord _function;
    private readonly Mock<FunctionContext> _context = new();
    private readonly Mock<HttpRequestData> _request;
    private readonly ValidateCohortDistributionRecordBody _requestBody;
    private readonly Mock<IDataServiceClient<CohortDistribution>> _cohortDistributionDataServiceMock = new();
    private readonly Mock<IOptions<ValidateCohortDistributionRecordConfig>> _config = new();


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

        var testConfig = new ValidateCohortDistributionRecordConfig
        {
            CohortDistributionDataServiceURL = "test",
            LookupValidationURL = "test2"
        };

        _config.Setup(c => c.Value).Returns(testConfig);

        _function = new ValidateCohortDistributionRecord(
            _logger.Object, 
            _createResponse.Object, 
            _exceptionHandler.Object, 
            _callFunction.Object, 
            _cohortDistributionDataServiceMock.Object,
            _config.Object
        );

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
    public async Task Run_BodyEmpty_BadRequest()
    {
        //Arrange
        _exceptionHandler.Setup(x => x.CreateSystemExceptionLogFromNhsNumber(It.IsAny<Exception>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Verifiable();
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
        It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
        Times.Once);


        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_RequestBodyInvalid_BadRequest()
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
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Once);

        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_LookupValidationCreatesValidationError_Created()
    {
        // Arrange
        SetUpRequestBody(JsonSerializer.Serialize(_requestBody));
        var existingParticipant = new CohortDistributionParticipant();

        _cohortDistributionDataServiceMock.Setup(x => x.GetSingle(It.IsAny<string>())).ReturnsAsync(new CohortDistribution());

        _callFunction.Setup(x => x.GetResponseText(It.IsAny<HttpWebResponse>())).Returns(
            Task.FromResult(JsonSerializer.Serialize(new ValidationExceptionLog()
            {
                IsFatal = true,
                CreatedException = true
            })
        ));

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.Created, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_LookupValidationDoesNotCreateValidationError_OK()
    {
        // Arrange
        SetUpRequestBody(JsonSerializer.Serialize(_requestBody));
        var existingParticipant = new CohortDistributionParticipant();
        _cohortDistributionDataServiceMock.Setup(x => x.GetSingle(It.IsAny<string>())).ReturnsAsync(new CohortDistribution());

        _callFunction.Setup(x => x.GetResponseText(It.IsAny<HttpWebResponse>())).Returns(
            Task.FromResult(JsonSerializer.Serialize(new ValidationExceptionLog()
            {
                IsFatal = false,
                CreatedException = false
            })
        ));

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_ExceptionOccursDuringValidation_InternalServerError()
    {
        // Arrange
        SetUpRequestBody(JsonSerializer.Serialize(_requestBody));
        var existingParticipant = new CohortDistributionParticipant();
        _cohortDistributionDataServiceMock.Setup(x => x.GetSingle(It.IsAny<string>())).ReturnsAsync(new CohortDistribution());

        _callFunction.Setup(x => x.SendPost(It.Is<string>(x => x.Contains("LookupValidationURL")), It.IsAny<string>()))
        .Throws(new Exception("some new exception"));

        _exceptionHandler.Setup(x => x.CreateSystemExceptionLogFromNhsNumber(It.IsAny<Exception>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
        .Verifiable();

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        _exceptionHandler.Verify(x => x.CreateSystemExceptionLogFromNhsNumber(It.IsAny<Exception>(), _requestBody.NhsNumber, _requestBody.FileName, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_ExceptionOccursDuringGettingParticipant_InternalServerError()
    {
        // Arrange
        var exception = new Exception("some new exception");

        SetUpRequestBody(JsonSerializer.Serialize(_requestBody));
        var existingParticipant = new CohortDistributionParticipant();


        _cohortDistributionDataServiceMock.Setup(x => x.GetSingle(It.IsAny<string>())).Throws(new Exception("some new exception"));

        _exceptionHandler.Setup(x => x.CreateSystemExceptionLogFromNhsNumber(It.IsAny<Exception>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
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
        _exceptionHandler.Verify(x => x.CreateSystemExceptionLogFromNhsNumber(It.IsAny<Exception>(), _requestBody.NhsNumber, _requestBody.FileName, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
    }

    private void SetUpRequestBody(string json)
    {
        var byteArray = Encoding.ASCII.GetBytes(json);
        var bodyStream = new MemoryStream(byteArray);

        _request.Setup(r => r.Body).Returns(bodyStream);
    }
}
