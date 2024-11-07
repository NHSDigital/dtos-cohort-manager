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
using System.Data;

[TestClass]
public class GetValidationExceptionsTests : DatabaseTestBaseSetup<GetValidationExceptions>
{
    private readonly List<ValidationException> _exceptionList;
    private readonly Dictionary<string, string> columnToClassPropertyMapping;

    public GetValidationExceptionsTests()
        : base((conn, logger, transaction, command, response) => null)
    {
        _exceptionList = new List<ValidationException>
        {
            new ValidationException { ExceptionId = 1 },
            new ValidationException { ExceptionId = 2 },
            new ValidationException { ExceptionId = 3 }
        };

        columnToClassPropertyMapping = new Dictionary<string, string>
        {
            { "EXCEPTION_ID", "ExceptionId" }
        };

        _service = new GetValidationExceptions(
            _loggerMock.Object,
            _createResponseMock.Object,
            _validationDataMock.Object,
            _httpParserHelperMock.Object
        );

        var json = JsonSerializer.Serialize(_exceptionList);
        SetupRequest(json);
        CreateHttpResponseMock();
        SetupDataReader(_exceptionList, columnToClassPropertyMapping);
    }

    [TestMethod]
    public void Run_NoExceptionIdQueryParameter_ReturnsAllExceptions()
    {
        // Arrange
        var exceptionId = 0;
        _validationDataMock.Setup(s => s.GetAllExceptions()).Returns(_exceptionList);
        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsInt(It.IsAny<HttpRequestData>(), It.IsAny<string>())).Returns(exceptionId);
        SetupRequestWithQueryParams([]);

        // Act
        var result = _service.Run(_request.Object);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        _validationDataMock.Verify(v => v.GetAllExceptions(), Times.Once);
    }

    [TestMethod]
    public void Run_ValidExceptionId_ReturnsExceptionById()
    {
        // Arrange
        var exceptionId = 1;
        _validationDataMock.Setup(s => s.GetExceptionById(exceptionId)).Returns(_exceptionList.First(f => f.ExceptionId == exceptionId));
        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsInt(It.IsAny<HttpRequestData>(), It.IsAny<string>())).Returns(exceptionId);
        SetupRequestWithQueryParams(new Dictionary<string, string> { { "exceptionId", exceptionId.ToString() } });

        // Act
        var result = _service.Run(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        _validationDataMock.Verify(v => v.GetExceptionById(exceptionId), Times.Once);
    }

    [TestMethod]
    public void Run_ExceptionIdIsOutOfRange_ReturnsNoContent()
    {
        // Arrange
        var exceptionId = 999;
        _validationDataMock.Setup(s => s.GetExceptionById(exceptionId)).Returns(new ValidationException());
        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsInt(It.IsAny<HttpRequestData>(), It.IsAny<string>())).Returns(exceptionId);

        SetupRequestWithQueryParams(new Dictionary<string, string> { { "exceptionId", exceptionId.ToString() } });

        // Act
        var result = _service.Run(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.NoContent, result.StatusCode);
        _validationDataMock.Verify(v => v.GetExceptionById(exceptionId), Times.Once);
    }
}
