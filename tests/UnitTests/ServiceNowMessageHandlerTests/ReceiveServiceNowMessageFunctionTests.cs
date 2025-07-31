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
using Model;
using Microsoft.Extensions.Options;

[TestClass]
public class ReceiveServiceNowMessageFunctionTests
{
    private readonly Mock<ILogger<ReceiveServiceNowMessageFunction>> _mockLogger = new();
    private readonly CreateResponse _createResponse = new();
    private readonly Mock<IQueueClient> _mockQueueClient = new();
    private readonly Mock<IOptions<ServiceNowMessageHandlerConfig>> _mockConfig = new();
    private readonly Mock<FunctionContext> _mockContext = new();
    private readonly Mock<HttpRequestData> _mockHttpRequest;
    private readonly ReceiveServiceNowMessageFunction _function;

    public ReceiveServiceNowMessageFunctionTests()
    {
        _mockConfig.Setup(x => x.Value).Returns(new ServiceNowMessageHandlerConfig
        {
            ServiceNowRefreshAccessTokenUrl = "https://www.example.net/refresh",
            ServiceNowUpdateUrl = "https://www.example.net/update",
            ServiceNowClientId = "123",
            ServiceNowClientSecret = "ABC",
            ServiceNowRefreshToken = "DEF",
            ServiceBusConnectionString_client_internal = "Endpoint=",
            ServiceNowParticipantManagementTopic = "servicenow-participant-management-topic"
        });
        _function = new ReceiveServiceNowMessageFunction(_mockLogger.Object, _createResponse, _mockQueueClient.Object, _mockConfig.Object);
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
    [DataRow("CS123", "1234567890", "Charlie", "Bloggs", "1970-01-01", "ABC", ServiceNowReasonsForAdding.VeryHighRisk, null)]
    [DataRow("CS123", "1234567890", "Charlie", "Bloggs", "1970-12-31", "ABC", ServiceNowReasonsForAdding.RequiresCeasing, "")]
    [DataRow("CS123", "1234567890", "Charlie", "Bloggs", "1970-01-01", "ABC", ServiceNowReasonsForAdding.RoutineScreening, "ZZZ")]
    public async Task Run_WhenRequestIsValidAndMessageSuccessfullySentToServiceBus_ReturnsAccepted(
        string caseNumber, string nhsNumber, string forename, string familyName, string dateOfBirth, string bsoCode, string reasonForAdding, string dummyGpCode)
    {
        // Arrange
        var requestBodyJson = CreateRequestBodyJson(caseNumber, nhsNumber, forename, familyName, dateOfBirth, bsoCode, reasonForAdding, dummyGpCode);
        var requestBodyStream = new MemoryStream(Encoding.UTF8.GetBytes(requestBodyJson));
        _mockHttpRequest.Setup(r => r.Body).Returns(requestBodyStream);
        _mockQueueClient.Setup(x => x.AddAsync(It.Is<ServiceNowParticipant>(p =>
                p.ScreeningId == 1 &&
                p.ServiceNowRecordNumber == caseNumber &&
                p.NhsNumber == long.Parse(nhsNumber) &&
                p.FirstName == forename &&
                p.FamilyName == familyName &&
                p.DateOfBirth.ToString("yyyy-MM-dd") == dateOfBirth &&
                p.BsoCode == bsoCode &&
                p.ReasonForAdding == reasonForAdding &&
                p.RequiredGpCode == dummyGpCode
            ),
            _mockConfig.Object.Value.ServiceNowParticipantManagementTopic))
            .ReturnsAsync(true);

        // Act
        var result = await _function.Run(_mockHttpRequest.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.Accepted, result.StatusCode);
    }

    [TestMethod]
    [DataRow("null")]
    [DataRow("{}")]
    [DataRow("Invalid json")]
    public async Task Run_WhenRequestBodyIsInvalidJsonOrEmpty_ReturnsBadRequest(string requestBodyJson)
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
    [DataRow(null, "1234567890", "Charlie", "Bloggs", "1970-01-01", "ABC", ServiceNowReasonsForAdding.VeryHighRisk)]    // Case Number null
    [DataRow("", "1234567890", "Charlie", "Bloggs", "1970-01-01", "ABC", ServiceNowReasonsForAdding.VeryHighRisk)]      // Case Number empty
    [DataRow("CS123", null, "Charlie", "Bloggs", "1970-01-01", "ABC", ServiceNowReasonsForAdding.VeryHighRisk)]         // NHS Number null
    [DataRow("CS123", "", "Charlie", "Bloggs", "1970-01-01", "ABC", ServiceNowReasonsForAdding.VeryHighRisk)]           // NHS Number empty
    [DataRow("CS123", "1234567890", null, "Bloggs", "1970-01-01", "ABC", ServiceNowReasonsForAdding.VeryHighRisk)]      // Forename null
    [DataRow("CS123", "1234567890", "", "Bloggs", "1970-01-01", "ABC", ServiceNowReasonsForAdding.VeryHighRisk)]        // Forename empty
    [DataRow("CS123", "1234567890", "Charlie", null, "1970-01-01", "ABC", ServiceNowReasonsForAdding.VeryHighRisk)]     // Family Name null
    [DataRow("CS123", "1234567890", "Charlie", "", "1970-01-01", "ABC", ServiceNowReasonsForAdding.VeryHighRisk)]       // Family Name empty
    [DataRow("CS123", "1234567890", "Charlie", "Bloggs", null, "ABC", ServiceNowReasonsForAdding.VeryHighRisk)]         // Date of Birth null
    [DataRow("CS123", "1234567890", "Charlie", "Bloggs", "", "ABC", ServiceNowReasonsForAdding.VeryHighRisk)]           // Date of Birth empty
    [DataRow("CS123", "1234567890", "Charlie", "Bloggs", "1970", "ABC", ServiceNowReasonsForAdding.VeryHighRisk)]       // Date of Birth invalid value
    [DataRow("CS123", "1234567890", "Charlie", "Bloggs", "1970-01-01", null, ServiceNowReasonsForAdding.VeryHighRisk)]  // BSO code null
    [DataRow("CS123", "1234567890", "Charlie", "Bloggs", "1970-01-01", "", ServiceNowReasonsForAdding.VeryHighRisk)]    // BSO code empty
    [DataRow("CS123", "1234567890", "Charlie", "Bloggs", "1970-01-01", "ABC", null)]                                    // Reason for adding null
    [DataRow("CS123", "1234567890", "Charlie", "Bloggs", "1970-01-01", "ABC", "")]                                      // Reason for adding empty
    [DataRow("CS123", "1234567890", "Charlie", "Bloggs", "1970-01-01", "ABC", "Invalid reason")]                        // Reason for adding invalid value
    public async Task Run_WhenMandatoryPropertyIsNullOrEmptyOrInvalidValue_ReturnsBadRequest(
        string caseNumber, string nhsNumber, string forename, string familyName, string dateOfBirth, string bsoCode, string reasonForAdding)
    {
        // Arrange
        var requestBodyJson = CreateRequestBodyJson(caseNumber, nhsNumber, forename, familyName, dateOfBirth, bsoCode, reasonForAdding);
        var requestBodyStream = new MemoryStream(Encoding.UTF8.GetBytes(requestBodyJson));
        _mockHttpRequest.Setup(r => r.Body).Returns(requestBodyStream);

        // Act
        var result = await _function.Run(_mockHttpRequest.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [TestMethod]
    [DataRow("CS123", "1234567890", "Charlie", "Bloggs", "1970-01-01", "ABC", ServiceNowReasonsForAdding.VeryHighRisk, null)]
    public async Task Run_WhenRequestIsValidButMessageFailsToSendToServiceBus_ReturnsInternalServiceError(
        string caseNumber, string nhsNumber, string forename, string familyName, string dateOfBirth, string bsoCode, string reasonForAdding, string dummyGpCode)
    {
        // Arrange
        var requestBodyJson = CreateRequestBodyJson(caseNumber, nhsNumber, forename, familyName, dateOfBirth, bsoCode, reasonForAdding, dummyGpCode);
        var requestBodyStream = new MemoryStream(Encoding.UTF8.GetBytes(requestBodyJson));
        _mockHttpRequest.Setup(r => r.Body).Returns(requestBodyStream);
        _mockQueueClient.Setup(x => x.AddAsync(It.Is<ServiceNowParticipant>(p =>
                p.ScreeningId == 1 &&
                p.ServiceNowRecordNumber == caseNumber &&
                p.NhsNumber == long.Parse(nhsNumber) &&
                p.FirstName == forename &&
                p.FamilyName == familyName &&
                p.DateOfBirth.ToString("yyyy-MM-dd") == dateOfBirth &&
                p.BsoCode == bsoCode &&
                p.ReasonForAdding == reasonForAdding &&
                p.RequiredGpCode == dummyGpCode
            ),
            _mockConfig.Object.Value.ServiceNowParticipantManagementTopic))
            .ReturnsAsync(false);

        // Act
        var result = await _function.Run(_mockHttpRequest.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
    }

    private static string CreateRequestBodyJson(
        string caseNumber, string nhsNumber, string forename, string familyName, string dateOfBirth, string bsoCode, string reasonForAdding, string? dummyGpCode = null)
    {
        var obj = new
        {
            number = caseNumber,
            u_case_variable_data = new
            {
                nhs_number = nhsNumber,
                forename_ = forename,
                surname_family_name = familyName,
                date_of_birth = dateOfBirth,
                BSO_code = bsoCode,
                reason_for_adding = reasonForAdding,
                enter_dummy_gp_code = dummyGpCode
            }
        };

        return JsonSerializer.Serialize(obj);
    }
}
