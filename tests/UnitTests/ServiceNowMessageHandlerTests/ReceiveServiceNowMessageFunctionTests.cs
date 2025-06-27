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
        var requestBodyJson = JsonSerializer.Serialize(new
        {
            nhs_number = "1234567890",
            forename = "Charlie",
            surname_family_name = "Bloggs",
            date_of_birth = "1970-01-01"
        });
        var requestBodyStream = new MemoryStream(Encoding.UTF8.GetBytes(requestBodyJson));
        _mockHttpRequest.Setup(r => r.Body).Returns(requestBodyStream);

        // Act
        var result = await _function.Run(_mockHttpRequest.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.Accepted, result.StatusCode);
    }

    [TestMethod]
    [DataRow("{\"forename\":\"Charlie\",\"surname_family_name\":\"Bloggs\",\"date_of_birth\":\"1970-01-01\"}")]                         // NHS Number missing
    [DataRow("{\"nhs_number\":null,\"forename\":\"Charlie\",\"surname_family_name\":\"Bloggs\",\"date_of_birth\":\"1970-01-01\"}")]     // NHS Number null
    [DataRow("{\"nhs_number\":\"\",\"forename\":\"Charlie\",\"surname_family_name\":\"Bloggs\",\"date_of_birth\":\"1970-01-01\"}")]     // NHS Number empty
    [DataRow("{\"nhs_number\":\"1234567890\",\"surname_family_name\":\"Bloggs\",\"date_of_birth\":\"1970-01-01\"}")]                    // Forename missing
    [DataRow("{\"nhs_number\":\"1234567890\",\"forename\":null,\"surname_family_name\":\"Bloggs\",\"date_of_birth\":\"1970-01-01\"}")]  // Forename null
    [DataRow("{\"nhs_number\":\"1234567890\",\"forename\":\"\",\"surname_family_name\":\"Bloggs\",\"date_of_birth\":\"1970-01-01\"}")]  // Forename empty
    [DataRow("{\"nhs_number\":\"1234567890\",\"forename\":\"Charlie\",\"date_of_birth\":\"1970-01-01\"}")]                              // Family Name missing
    [DataRow("{\"nhs_number\":\"1234567890\",\"forename\":\"\",\"surname_family_name\":null,\"date_of_birth\":\"1970-01-01\"}")]        // Family Name null
    [DataRow("{\"nhs_number\":\"1234567890\",\"forename\":\"\",\"surname_family_name\":\"\",\"date_of_birth\":\"1970-01-01\"}")]        // Family Name empty
    [DataRow("{\"nhs_number\":\"1234567890\",\"forename\":\"\",\"surname_family_name\":\"Bloggs\",}")]                                  // Date of Birth missing
    [DataRow("{\"nhs_number\":\"1234567890\",\"forename\":\"\",\"surname_family_name\":\"Bloggs\",\"date_of_birth\":null}")]            // Date of Birth null
    [DataRow("{\"nhs_number\":\"1234567890\",\"forename\":\"\",\"surname_family_name\":\"Bloggs\",\"date_of_birth\":\"\"}")]            // Date of Birth empty
    [DataRow("{\"nhs_number\":\"1234567890\",\"forename\":\"\",\"surname_family_name\":\"Bloggs\",\"date_of_birth\":\"01-01-1980\"}")]  // Date of Birth incorrect date format
    public async Task Run_WhenMandatoryPropertyIsMissingOrNullOrEmpty_ReturnsBadRequest(string requestBodyJson)
    {
        // Arrange
        var requestBodyStream = new MemoryStream(Encoding.UTF8.GetBytes(requestBodyJson));
        _mockHttpRequest.Setup(r => r.Body).Returns(requestBodyStream);

        // Act
        var result = await _function.Run(_mockHttpRequest.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
    }
}
