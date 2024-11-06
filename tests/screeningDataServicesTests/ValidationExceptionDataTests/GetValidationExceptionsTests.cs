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

    public GetValidationExceptionsTests(): base((conn, logger, transaction, command, response) =>
            new GetValidationExceptions(logger, response, null, null, null))
    {
        _exceptionList = new List<ValidationException>
        {
            new ValidationException { ExceptionId = 1 },
            new ValidationException { ExceptionId = 2 }
        };

        var json = JsonSerializer.Serialize(_exceptionList);
        _request = SetupRequest(json);
        _createResponseMock = CreateHttpResponseMock();
        _function = new GetValidationExceptions(_loggerMock.Object, _createResponseMock.Object, _validationDataMock.Object, _exceptionHandlerMock.Object, _httpParserHelperMock.Object);
        _service = new ValidationExceptionData(_mockDBConnection.Object,_serviceLoggerMock.Object);
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
        //Arrange
        // _mockDataReader.SetupSequence(reader => reader.Read()).Returns(true).Returns(false);
        //Act
        var result = _service.GetAllExceptions();

        //Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType<List<ValidationException>>(result);
    }

[TestMethod]
public void GetAllExceptions_NoExceptionId_ReturnsAllExceptions2() //wp this is not reading
{
    // Arrange
    // Setup the mock IDataReader to return data from _exceptionList
    _mockDataReader.SetupSequence(reader => reader.Read())
                   .Returns(true)  // First call to Read, returns true (first row of data)
                   .Returns(true)  // Second call to Read, returns true (second row of data)
                   .Returns(false); // Third call to Read, returns false (no more data)

    // Mock the columns returned by the IDataReader for each row.
    // First row returns the first ExceptionId
    _mockDataReader.Setup(reader => reader["ExceptionId"]).Returns(() => _exceptionList[0].ExceptionId);

    // Second row returns the second ExceptionId
    _mockDataReader.Setup(reader => reader["ExceptionId"]).Returns(() => _exceptionList[1].ExceptionId);

    // Setup mock database command to return the mocked reader
    _commandMock.Setup(m => m.ExecuteReader()).Returns(_mockDataReader.Object);

    // Act
    var result = _service.GetAllExceptions();

    // Assert
    Assert.IsNotNull(result);
    Assert.IsInstanceOfType(result, typeof(List<ValidationException>));

    // Verify that the result contains 2 exceptions
    Assert.AreEqual(2, result.Count);

    // Verify that the returned exceptions match the ExceptionIds from the _exceptionList
    Assert.AreEqual(1, result[0].ExceptionId);  // The first ExceptionId should be 1
    Assert.AreEqual(2, result[1].ExceptionId);  // The second ExceptionId should be 2
}

}
