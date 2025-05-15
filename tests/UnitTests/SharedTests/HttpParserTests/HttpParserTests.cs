namespace HttpParserHelperTests;

using Common;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker;
using System.Collections.Specialized;
using Model;

[TestClass]
public class HttpParserHelperTests
{
    private readonly Mock<ILogger<HttpParserHelper>> _logger = new();
    private readonly HttpParserHelper _httpParserHelper;
    private readonly Mock<ICreateResponse> _createResponse = new();
    private readonly Mock<HttpRequestData> _request;
    private readonly Mock<FunctionContext> _context = new();

    public HttpParserHelperTests()
    {
        _httpParserHelper = new HttpParserHelper(_logger.Object, _createResponse.Object);

        _request = new Mock<HttpRequestData>(_context.Object);

        _request.Setup(r => r.CreateResponse()).Returns(() =>
            {
                var response = new Mock<HttpResponseData>(_context.Object);
                response.SetupProperty(r => r.Headers, new HttpHeadersCollection());
                response.SetupProperty(r => r.StatusCode);
                response.SetupProperty(r => r.Body, new MemoryStream());

                return response.Object;
            });
    }

    [TestMethod]
    [DataRow("ID", "1", 1)]
    [DataRow("ID", "0", 0)]
    public void GetQueryParameterAsInt_ValidInput_ReturnInt(string key, string value, int expected)
    {
        // Arrange
        _request.Setup(x => x.Query).Returns(new NameValueCollection() { { key, value } });

        //Act
        var actual = _httpParserHelper.GetQueryParameterAsInt(_request.Object, key);

        // Assert
        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    [DataRow("ID", "1", 0)]
    public void GetQueryParameterAsInt_InvalidInput_ReturnDefaultValue(string key, string value, int defaultValue)
    {
        // Arrange
        string invalidKey = "InvalidInput";
        _request.Setup(x => x.Query).Returns(new NameValueCollection() { { invalidKey, value } });

        //Act
        var actual = _httpParserHelper.GetQueryParameterAsInt(_request.Object, key);

        // Assert
        Assert.AreEqual(defaultValue, actual);
    }

    [TestMethod]
    [DataRow("ID", null, 0)]
    [DataRow(null, "ABC", 0)]
    public void GetQueryParameterAsInt_NullInput_ReturnDefaultValue(string key, string value, int defaultValue)
    {
        // Arrange
        _request.Setup(x => x.Query).Returns(new NameValueCollection() { { key, value } });

        //Act
        var actual = _httpParserHelper.GetQueryParameterAsInt(_request.Object, key);

        // Assert
        Assert.AreEqual(defaultValue, actual);
    }
    [TestMethod]
    [DataRow("No Patient ID")]
    public void LogErrorResponse_ValidError_ReturnHttpErrorResponse(string errorMessage)
    {
        //No Arrange
        //Act
        _httpParserHelper.LogErrorResponse(_request.Object, errorMessage);

        //Assert
        _logger.Verify(x => x.Log(It.Is<LogLevel>(l => l == LogLevel.Error),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(errorMessage)),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()));
    }


    [TestMethod]
    [DataRow("ID", "1", true)]
    [DataRow("ID", "0", false)]
    public void GetQueryParameterAsBool_ValidInput_ReturnBool(string key, string value, bool expected)
    {
        // Arrange
        bool defaultValue = false;
        _request.Setup(x => x.Query).Returns(new NameValueCollection() { { key, value } });

        //Act
        var actual = _httpParserHelper.GetQueryParameterAsBool(_request.Object, key, defaultValue);

        // Assert
        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    [DataRow("ID", null, true)]
    [DataRow("ID", null, false)]
    public void GetQueryParameterAsBool_InvalidInput_ReturnDefaultValue(string key, string value, bool defaultValue)
    {
        // Arrange
        string invalidKey = "InvalidInput";
        _request.Setup(x => x.Query).Returns(new NameValueCollection() { { invalidKey, value } });

        //Act
        var actual = _httpParserHelper.GetQueryParameterAsBool(_request.Object, key, defaultValue);

        // Assert
        Assert.AreEqual(defaultValue, actual);
    }

    [TestMethod]
    [DataRow("ID", null, true)]
    [DataRow("ID", null, false)]
    [DataRow(null, "ABC", true)]
    [DataRow(null, "ABC", false)]
    public void GetQueryParameterAsBool_NullInput_ReturnDefaultValue(string key, string value, bool defaultValue)
    {
        // Arrange
        _request.Setup(x => x.Query).Returns(new NameValueCollection() { { key, value } });

        //Act
        var actual = _httpParserHelper.GetQueryParameterAsBool(_request.Object, key, defaultValue);

        // Assert
        Assert.AreEqual(defaultValue, actual);
    }
}
