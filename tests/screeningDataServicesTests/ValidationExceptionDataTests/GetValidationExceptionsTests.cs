namespace NHS.CohortManager.Tests.ScreeningDataServicesTests;

using Common.Interfaces;
using Data.Database;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using Model;
using NHS.CohortManager.ScreeningDataServices;
using Common;
using Moq;
using NHS.CohortManager.Tests.TestUtils;
using System.Text.Json;
using Microsoft.Extensions.Logging;

[TestClass]
public class GetValidationExceptionsTests : DatabaseTestBaseSetup<GetValidationExceptions>
{
    private readonly Mock<ILogger<ValidationExceptionData>> _serviceLoggerMock = new();
    private readonly List<ValidationException> _exceptionList;
    private readonly GetValidationExceptions _function;
    private readonly ValidationExceptionData _service;
    private readonly Mock<IValidationExceptionData> _validationDataMock = new();
    private readonly Mock<IHttpParserHelper> _httpParserHelperMock = new();
    private readonly Mock<IExceptionHandler> _exceptionHandlerMock = new();
    private readonly Dictionary<string, string> columnToPropertyMapping = [];

    public GetValidationExceptionsTests() : base((conn, logger, transaction, command, response) =>
            new GetValidationExceptions(logger, response, null, null, null))
    {
        _exceptionList = new List<ValidationException>
        {
            new ValidationException { ExceptionId = 1 },
            new ValidationException { ExceptionId = 2 },
            new ValidationException { ExceptionId = 3 }
        };

        columnToPropertyMapping = new Dictionary<string, string>
    {
        { "EXCEPTION_ID", "ExceptionId" }
    };
        var json = JsonSerializer.Serialize(_exceptionList);
        SetupRequest(json);
        CreateHttpResponseMock();
        _function = new GetValidationExceptions(_loggerMock.Object, _createResponseMock.Object, _validationDataMock.Object, _exceptionHandlerMock.Object, _httpParserHelperMock.Object);
        _service = new ValidationExceptionData(_mockDBConnection.Object, _serviceLoggerMock.Object);
    }

    [TestMethod]
    public void Run_NoExceptionIdQueryParameter_ReturnsAllExceptions()
    {
        // Arrange
        _validationDataMock.Setup(s => s.GetAllExceptions()).Returns(_exceptionList);
        SetupRequestWithQueryParams([]);

        // Act
        var result = _function.Run(_request.Object);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        _validationDataMock.Verify(v => v.GetAllExceptions(), Times.Once);
    }

    [TestMethod]
    public void Run_ValidExceptionId_ReturnsSpecificException()
    {
        // Arrange
        var exceptionId = 1;
        _validationDataMock.Setup(s => s.GetExceptionById(exceptionId)).Returns(_exceptionList.First(f => f.ExceptionId == exceptionId));
        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsInt(It.IsAny<HttpRequestData>(), It.IsAny<string>())).Returns(exceptionId);
        SetupRequestWithQueryParams(new Dictionary<string, string> { { "exceptionId", exceptionId.ToString() } });

        // Act
        var result = _function.Run(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        _validationDataMock.Verify(v => v.GetExceptionById(exceptionId), Times.Once);
    }

    [TestMethod]
    public void Run_InvalidExceptionId_ReturnsNoContent()
    {
        // Arrange
        var exceptionId = 999;
        _validationDataMock.Setup(s => s.GetExceptionById(exceptionId)).Returns(new ValidationException());
        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsInt(It.IsAny<HttpRequestData>(), It.IsAny<string>())).Returns(exceptionId);
        SetupRequestWithQueryParams(new Dictionary<string, string> { { "exceptionId", exceptionId.ToString() } });

        // Act
        var result = _function.Run(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.NoContent, result.StatusCode);
        _validationDataMock.Verify(v => v.GetExceptionById(exceptionId), Times.Once);
    }

    [TestMethod]
    public void GetAllExceptions_NoExceptionId_ReturnsAllExceptions()
    {
        // Arrange
        SetupDataReader(_exceptionList, columnToPropertyMapping);

        // Act
        var result = _service.GetAllExceptions();

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(List<ValidationException>));
        Assert.AreEqual(3, result.Count);
        Assert.AreEqual(1, result[0].ExceptionId);
        Assert.AreEqual(2, result[1].ExceptionId);
        Assert.AreEqual(3, result[2].ExceptionId);
    }

}
