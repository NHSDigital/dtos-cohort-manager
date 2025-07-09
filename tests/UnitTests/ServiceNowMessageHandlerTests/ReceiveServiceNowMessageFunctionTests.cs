namespace NHS.CohortManager.Tests.UnitTests.ServiceNowMessageHandlerTests;

using Moq;
using Common;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using NHS.CohortManager.ServiceNowIntegrationService;
using Microsoft.Azure.Functions.Worker;
using System.Net;
using System.Text.Json;
using System.Text;

[TestClass]
public class ReceiveServiceNowMessageFunctionTests
{
    private readonly Mock<ILogger<ReceiveServiceNowMessageFunction>> _mockLogger = new();
    private readonly CreateResponse _createResponse = new();
    private readonly Mock<FunctionContext> _mockContext = new();
    private readonly Mock<HttpRequestData> _mockHttpRequest;
    private readonly ReceiveServiceNowMessageFunction _function;

    public ReceiveServiceNowMessageFunctionTests()
    {
        _function = new ReceiveServiceNowMessageFunction(_mockLogger.Object, _createResponse);
        _mockHttpRequest = new Mock<HttpRequestData>(_mockContext.Object);

        _mockHttpRequest.Setup(r => r.CreateResponse()).Returns(() =>
        {
            var response = new Mock<HttpResponseData>(_mockContext.Object);
            response.SetupProperty(r => r.Headers, new HttpHeadersCollection());
            response.SetupProperty(r => r.StatusCode);
            response.SetupProperty(r => r.Body, new MemoryStream());
            return response.Object;
        });
    }

    [TestMethod]
    public async Task Run_WhenRequestIsValid_ReturnsAccepted()
    {
        // Arrange
        var requestBodyJson = CreateRequestBodyJson("1234567890", "Charlie", "Bloggs", "1970-01-01", "ABC");
        var requestBodyStream = new MemoryStream(Encoding.UTF8.GetBytes(requestBodyJson));
        _mockHttpRequest.Setup(r => r.Body).Returns(requestBodyStream);

        // Act
        var result = await _function.Run(_mockHttpRequest.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.Accepted, result.StatusCode);
    }

    [TestMethod]
    [DataRow("{}")]
    [DataRow("{\"forename_\":\"Charlie\",\"surname_family_name\":\"Bloggs\",\"date_of_birth\":\"1970-01-01\",\"BSO_code\":\"ABC\"}")]           // NHS Number missing
    [DataRow("{\"nhs_number\":\"1234567890\",\"surname_family_name\":\"Bloggs\",\"date_of_birth\":\"1970-01-01\",\"BSO_code\":\"ABC\"}")]       // Forename missing
    [DataRow("{\"nhs_number\":\"1234567890\",\"forename_\":\"Charlie\",\"date_of_birth\":\"1970-01-01\",\"BSO_code\":\"ABC\"}")]                // Family Name missing
    [DataRow("{\"nhs_number\":\"1234567890\",\"forename_\":\"Charlie\",\"surname_family_name\":\"Bloggs\",\"BSO_code\":\"ABC\"}")]              // Date of Birth missing
    [DataRow("{\"nhs_number\":\"1234567890\",\"forename_\":\"Charlie\",\"surname_family_name\":\"Bloggs\",\"date_of_birth\":\"01-01-1980\"}")]  // BSO Code missing
    public async Task Run_WhenMandatoryPropertyIsMissing_ReturnsBadRequest(string requestBodyJson)
    {
        // Arrange
        var requestBodyStream = new MemoryStream(Encoding.UTF8.GetBytes(requestBodyJson));
        _mockHttpRequest.Setup(r => r.Body).Returns(requestBodyStream);

        // Act
        var result = await _function.Run(_mockHttpRequest.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [TestMethod]
    [DataRow(null, "Charlie", "Bloggs", "1970-01-01", "ABC")]           // NHS Number null
    [DataRow("", "Charlie", "Bloggs", "1970-01-01", "ABC")]             // NHS Number empty
    [DataRow("1234567890", null, "Bloggs", "1970-01-01", "ABC")]        // Forename null
    [DataRow("1234567890", "", "Bloggs", "1970-01-01", "ABC")]          // Forename empty
    [DataRow("1234567890", "Charlie", null, "1970-01-01", "ABC")]       // Family Name null
    [DataRow("1234567890", "Charlie", "", "1970-01-01", "ABC")]         // Family Name empty
    [DataRow("1234567890", "Charlie", "Bloggs", null, "ABC")]           // Date of Birth null
    [DataRow("1234567890", "Charlie", "Bloggs", "", "ABC")]             // Date of Birth empty
    [DataRow("1234567890", "Charlie", "Bloggs", "1970", "ABC")]         // Date of Birth invalid value
    [DataRow("1234567890", "Charlie", "Bloggs", "1970-01-01", null)]    // BSO code null
    [DataRow("1234567890", "Charlie", "Bloggs", "1970-01-01", "")]      // BSO code empty
    public async Task Run_WhenMandatoryPropertyIsNullOrEmptyOrInvalidValue_ReturnsBadRequest(
        string nhsNumber, string forename, string familyName, string dateOfBirth, string bsoCode)
    {
        // Arrange
        var requestBodyJson = CreateRequestBodyJson(nhsNumber, forename, familyName, dateOfBirth, bsoCode);
        var requestBodyStream = new MemoryStream(Encoding.UTF8.GetBytes(requestBodyJson));
        _mockHttpRequest.Setup(r => r.Body).Returns(requestBodyStream);

        // Act
        var result = await _function.Run(_mockHttpRequest.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
    }

    private static string CreateRequestBodyJson(string nhsNumber, string forename, string familyName, string dateOfBirth, string bsoCode)
    {
        var obj = new
        {
            nhs_number = nhsNumber,
            forename_ = forename,
            surname_family_name = familyName,
            date_of_birth = dateOfBirth,
            BSO_code = bsoCode
        };

        return JsonSerializer.Serialize(obj);
    }
}
