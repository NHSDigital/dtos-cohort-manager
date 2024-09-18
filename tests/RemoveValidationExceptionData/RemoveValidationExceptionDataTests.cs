using System.Net;
using System.Text;
using System.Text.Json;
using Common;
using Data.Database;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Model;
using Moq;
using NHS.CohortManager.ScreeningValidationService;

namespace NHS.CohortManager.Tests.ScreeningDataServicesTests;

[TestClass]
public class RemoveValidationExceptionDataTests
{
    private readonly Mock<ICreateResponse> _createResponse = new();

    private readonly Mock<FunctionContext> _context = new();
    private readonly Mock<IExceptionHandler> _handleException = new();
    private Mock<HttpRequestData> _request;
    private readonly ServiceCollection _serviceCollection = new();
    private Mock<ICallFunction> _callFunction = new();

    private Mock<IValidationExceptionData> _validationExceptionData = new();

    private RemoveValidationExceptionData _removeValidationExceptionData;

    private readonly Mock<ILogger<RemoveValidationExceptionData>> _logger = new();

    private readonly OldExceptionRecord _participantCsvRecord;

    public RemoveValidationExceptionDataTests()
    {


        Environment.SetEnvironmentVariable("CreateValidationExceptionURL", "CreateValidationExceptionURL");

        _request = new Mock<HttpRequestData>(_context.Object);

        var serviceProvider = _serviceCollection.BuildServiceProvider();

        _context.SetupProperty(c => c.InstanceServices, serviceProvider);

        _removeValidationExceptionData = new RemoveValidationExceptionData(_createResponse.Object, _handleException.Object, _validationExceptionData.Object, _logger.Object);

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

        _participantCsvRecord = new OldExceptionRecord()
        {
            NhsNumber = "1111111",
            ScreeningName = "Breast Screening"
        };
    }


    [TestMethod]
    public async Task Run_Should_Return_OK_When_Exception_Is_Not_Removed()
    {
        // Act
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);
        var result = await _removeValidationExceptionData.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_Should_Return_Create_When_Exception_Is_Removed()
    {
        // Act
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        _validationExceptionData.Setup(x => x.RemoveOldException(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
        var result = await _removeValidationExceptionData.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.Created, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_Should_Return_InternalServerError_When_Exception_Is_Thrown()
    {
        // Act
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        SetUpRequestBody(json);

        _validationExceptionData.Setup(x => x.RemoveOldException(It.IsAny<string>(), It.IsAny<string>()))
        .Throws(new Exception("some new exception"));

        var result = await _removeValidationExceptionData.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
    }

    private void SetUpRequestBody(string json)
    {
        var byteArray = Encoding.ASCII.GetBytes(json);
        var bodyStream = new MemoryStream(byteArray);

        _request.Setup(r => r.Body).Returns(bodyStream);
    }
}
