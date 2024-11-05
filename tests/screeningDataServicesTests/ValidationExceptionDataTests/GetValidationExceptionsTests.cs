namespace NHS.CohortManager.Tests.ScreeningDataServicesTests;

using Common.Interfaces;
using Data.Database;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using Model;
using NHS.CohortManager.ScreeningDataServices;
using Common;
using Moq;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using System.Data;

[TestClass]
public class GetValidationExceptionsTests
{
    private readonly Mock<ILogger<GetValidationExceptions>> _loggerMock = new();
    private readonly Mock<ILogger<ValidationExceptionData>> _serviceloggerMock = new();
    private readonly Mock<ICreateResponse> _createResponseMock = new();
    private readonly Mock<IValidationExceptionData> _validationDataMock = new();
    private readonly Mock<IHttpParserHelper> _httpParserHelperMock = new();
    private readonly Mock<IExceptionHandler> _exceptionHandlerMock = new();
    private readonly GetValidationExceptions _function;
    private readonly Mock<HttpRequestData> _request;
    private readonly Mock<FunctionContext> _context = new();
    private readonly Mock<IDbConnection> _mockDBConnection = new();
    private readonly List<ValidationException> _exceptionList;

    private readonly ValidationExceptionData _service;
    public GetValidationExceptionsTests()
    {
        _request = new Mock<HttpRequestData>(_context.Object);

        _service = new ValidationExceptionData(_mockDBConnection.Object, _serviceloggerMock.Object);
        _function = new GetValidationExceptions(_loggerMock.Object,_createResponseMock.Object,_validationDataMock.Object,_exceptionHandlerMock.Object,_httpParserHelperMock.Object);

        _exceptionList =
        [
            new ValidationException { ExceptionId = 1 },
            new ValidationException { ExceptionId = 2 }
        ];

        _request.Setup(r => r.CreateResponse()).Returns(() =>
        {
            var response = new Mock<HttpResponseData>(_context.Object);
            response.SetupProperty(r => r.Headers, new HttpHeadersCollection());
            response.SetupProperty(r => r.StatusCode);
            response.SetupProperty(r => r.Body, new MemoryStream());
            return response.Object;
        });

        _createResponseMock.Setup(x => x.CreateHttpResponse(It.IsAny<HttpStatusCode>(), It.IsAny<HttpRequestData>(), It.IsAny<string>()))
            .Returns((HttpStatusCode statusCode, HttpRequestData req, string ResponseBody) =>
            {
                var response = req.CreateResponse(statusCode);
                response.Headers.Add("Content-Type", "application/json; charset=utf-8");
                response.WriteString(ResponseBody);
                return response;
            });

    }

    private void SetupRequestWithQueryParams(Dictionary<string, string> queryParams)
    {
        var queryCollection = new System.Collections.Specialized.NameValueCollection();
        foreach (var param in queryParams)
        {
            queryCollection.Add(param.Key, param.Value);
        }

        _request.Setup(x => x.Query).Returns(queryCollection);
    }

    [TestMethod]
    public void Run_ExceptionIdZero_ReturnsAllExceptions()
    {
        // Arrange

        _validationDataMock.Setup(x => x.GetAllExceptions()).Returns(_exceptionList);

        SetupRequestWithQueryParams(new Dictionary<string, string>
        {
            { "exceptionId", "0" }
        });

        // Act
        var result = _function.Run(_request.Object);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        _validationDataMock.Verify(x => x.GetAllExceptions(), Times.Once);

    }

    [TestMethod]
    public void Run_ValidExceptionId_ReturnsSpecificException()
    {
        // Arrange
        var exceptionId = 1;
        _validationDataMock.Setup(x => x.GetExceptionById(exceptionId)).Returns(_exceptionList.First(f => f.ExceptionId == exceptionId));
        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsInt(It.IsAny<HttpRequestData>(), It.IsAny<string>())).Returns(exceptionId);

        SetupRequestWithQueryParams(new Dictionary<string, string>
        {
            { "exceptionId", exceptionId.ToString() }
        });


        // Act
        var result = _function.Run(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        _validationDataMock.Verify(x => x.GetExceptionById(exceptionId), Times.Once);
    }

    [TestMethod]
    public void Run_InvalidExceptionId_ReturnsNoContent()
    {
        // Arrange
        var exceptionId = 999;
        _validationDataMock.Setup(x => x.GetExceptionById(exceptionId)).Returns(new ValidationException());
        _httpParserHelperMock.Setup(s => s.GetQueryParameterAsInt(It.IsAny<HttpRequestData>(), It.IsAny<string>())).Returns(exceptionId);

        SetupRequestWithQueryParams(new Dictionary<string, string>
        {
            { "exceptionId", exceptionId.ToString() }
        });

        // Act
        var result = _function.Run(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.NoContent, result.StatusCode);
        _validationDataMock.Verify(x => x.GetExceptionById(exceptionId), Times.Once);
    }

    [TestMethod]
    public void GetAllExceptions_NoExceptionId_ReturnsAllExceptions()
    {
        //Arrange
        var exceptionId = 0;

        //Act
        var result = _service.GetAllExceptions();

        //Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType<ValidationException>(result);
        _validationDataMock.Verify(x => x.GetAllExceptions(), Times.Once);

    }
}
