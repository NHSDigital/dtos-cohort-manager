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
using DataServices.Client;
using Model.Constants;

[TestClass]
public class ReceiveServiceNowMessageFunctionTests
{
    private readonly Mock<ILogger<ReceiveServiceNowMessageFunction>> _mockLogger = new();
    private readonly CreateResponse _createResponse = new();
    private readonly Mock<IQueueClient> _mockQueueClient = new();
    private readonly Mock<IOptions<ServiceNowMessageHandlerConfig>> _mockConfig = new();
    private readonly Mock<FunctionContext> _mockContext = new();
    private readonly Mock<HttpRequestData> _mockHttpRequest;
    private Mock<IDataServiceClient<ServicenowCase>> _mockServiceNowCasesClient = new();
    private readonly Mock<IServiceNowClient> _mockServiceNowClient = new();
    private readonly ReceiveServiceNowMessageFunction _function;

    public ReceiveServiceNowMessageFunctionTests()
    {
        _mockConfig.Setup(x => x.Value).Returns(new ServiceNowMessageHandlerConfig
        {
            ServiceNowCasesDataServiceURL = "",
            ServiceNowRefreshAccessTokenUrl = "https://www.example.net/refresh",
            ServiceNowUpdateUrl = "https://www.example.net/update",
            ServiceNowResolutionUrl = "https://www.example.net/resolution",
            ServiceNowGrantType = "refresh_token",
            ServiceNowClientId = "123",
            ServiceNowClientSecret = "ABC",
            ServiceNowRefreshToken = "DEF",
            ServiceBusConnectionString_client_internal = "Endpoint=",
            ServiceNowParticipantManagementTopic = "servicenow-participant-management-topic"
        });
        _function = new ReceiveServiceNowMessageFunction(_mockLogger.Object, _createResponse, _mockQueueClient.Object, _mockConfig.Object, _mockServiceNowCasesClient.Object, _mockServiceNowClient.Object);
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
    [DataRow("CS123", "9434765919", "Charlie", "Bloggs", "1970-01-01", "ABC", ServiceNowReasonsForAdding.VeryHighRisk, null)]
    [DataRow("CS123", "9434765919", "Charlie", "Bloggs", "1970-12-31", "ABC", ServiceNowReasonsForAdding.RequiresCeasing, "")]
    [DataRow("CS123", "9434765919", "Charlie", "Bloggs", "1970-01-01", "ABC", ServiceNowReasonsForAdding.RoutineScreening, "ZZZ")]
    [DataRow("CS123", "9434765919", "Charlie", "Bloggs", "1985-06-15", "ABC", ServiceNowReasonsForAdding.OverAgeSelfReferral, "ZZZ")]
    public async Task Run_WhenRequestIsValidAndCaseSuccessfullySavedToDbAndMessageSuccessfullySentToServiceBus_ReturnsAccepted(
        string caseNumber, string nhsNumber, string forename, string familyName, string dateOfBirth, string bsoCode, string reasonForAdding, string dummyGpCode)
    {
        // Arrange
        var requestBodyJson = CreateRequestBodyJson(caseNumber, nhsNumber, forename, familyName, dateOfBirth, bsoCode, reasonForAdding, dummyGpCode);
        var requestBodyStream = new MemoryStream(Encoding.UTF8.GetBytes(requestBodyJson));
        _mockHttpRequest.Setup(r => r.Body).Returns(requestBodyStream);
        _mockServiceNowCasesClient.Setup(x => x.Add(It.Is<ServicenowCase>(c =>
                c.ServicenowId == caseNumber &&
                c.NhsNumber == long.Parse(nhsNumber) &&
                c.Status == ServiceNowStatus.New
            ))).ReturnsAsync(true);
        _mockQueueClient.Setup(x => x.AddAsync(It.Is<ServiceNowParticipant>(p =>
                p.ScreeningId == 1 &&
                p.ServiceNowCaseNumber == caseNumber &&
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
    [DataRow("CS123", "9434765919", "Charlie", "Bloggs", "1970-01-01", "ABC", ServiceNowReasonsForAdding.VeryHighRisk, null)]
    public async Task Run_WhenRequestIsValidButMessageFailsToSaveToDb_ReturnsInternalServiceError(
        string caseNumber, string nhsNumber, string forename, string familyName, string dateOfBirth, string bsoCode, string reasonForAdding, string dummyGpCode)
    {
        // Arrange
        var requestBodyJson = CreateRequestBodyJson(caseNumber, nhsNumber, forename, familyName, dateOfBirth, bsoCode, reasonForAdding, dummyGpCode);
        var requestBodyStream = new MemoryStream(Encoding.UTF8.GetBytes(requestBodyJson));
        _mockHttpRequest.Setup(r => r.Body).Returns(requestBodyStream);
        _mockServiceNowCasesClient.Setup(x => x.Add(It.Is<ServicenowCase>(c =>
        c.ServicenowId == caseNumber &&
        c.NhsNumber == long.Parse(nhsNumber) &&
        c.Status == ServiceNowStatus.New
    ))).ReturnsAsync(false);

        // Act
        var result = await _function.Run(_mockHttpRequest.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
        _mockQueueClient.VerifyNoOtherCalls();
    }

    [TestMethod]
    [DataRow("CS123", "9434765919", "Charlie", "Bloggs", "1970-01-01", "ABC", ServiceNowReasonsForAdding.VeryHighRisk, null)]
    public async Task Run_WhenRequestIsValidButMessageFailsToSendToServiceBus_ReturnsInternalServiceError(
        string caseNumber, string nhsNumber, string forename, string familyName, string dateOfBirth, string bsoCode, string reasonForAdding, string dummyGpCode)
    {
        // Arrange
        var requestBodyJson = CreateRequestBodyJson(caseNumber, nhsNumber, forename, familyName, dateOfBirth, bsoCode, reasonForAdding, dummyGpCode);
        var requestBodyStream = new MemoryStream(Encoding.UTF8.GetBytes(requestBodyJson));
        _mockHttpRequest.Setup(r => r.Body).Returns(requestBodyStream);
        _mockServiceNowCasesClient.Setup(x => x.Add(It.Is<ServicenowCase>(c =>
        c.ServicenowId == caseNumber &&
        c.NhsNumber == long.Parse(nhsNumber) &&
        c.Status == ServiceNowStatus.New
    ))).ReturnsAsync(true);
        _mockQueueClient.Setup(x => x.AddAsync(It.Is<ServiceNowParticipant>(p =>
                p.ScreeningId == 1 &&
                p.ServiceNowCaseNumber == caseNumber &&
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

    [TestMethod]
    [DataRow("CS123", "1234567891", "Charlie", "Bloggs", "1970-01-01", "ABC", ServiceNowReasonsForAdding.VeryHighRisk, null, DisplayName = "Invalid checksum")]
    [DataRow("CS123", "0000000000", "Charlie", "Bloggs", "1970-01-01", "ABC", ServiceNowReasonsForAdding.VeryHighRisk, null, DisplayName = "Nil return file NHS number")]
    [DataRow("CS123", "1234567892", "Charlie", "Bloggs", "1970-01-01", "ABC", ServiceNowReasonsForAdding.VeryHighRisk, null, DisplayName = "Invalid checksum")]
    public async Task Run_WhenNhsNumberFailsChecksumValidation_ReturnsBadRequestAndResolvesServiceNowTicket(
        string caseNumber, string nhsNumber, string forename, string familyName, string dateOfBirth, string bsoCode, string reasonForAdding, string? dummyGpCode)
    {
        // Arrange
        var requestBodyJson = CreateRequestBodyJson(caseNumber, nhsNumber, forename, familyName, dateOfBirth, bsoCode, reasonForAdding, dummyGpCode);
        var requestBodyStream = new MemoryStream(Encoding.UTF8.GetBytes(requestBodyJson));
        _mockHttpRequest.Setup(r => r.Body).Returns(requestBodyStream);

        _mockServiceNowClient
            .Setup(x => x.SendResolution(caseNumber, It.Is<string>(msg => msg.Contains("could not add"))))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK))
            .Verifiable();

        // Act
        var result = await _function.Run(_mockHttpRequest.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
        _mockServiceNowClient.Verify();
        _mockServiceNowCasesClient.VerifyNoOtherCalls();
        _mockQueueClient.VerifyNoOtherCalls();
    }

    [TestMethod]
    [DataRow("CS123", "9434765919", "Charlie", "Bloggs", "1970-01-01", "ABC", ServiceNowReasonsForAdding.VeryHighRisk, null, DisplayName = "Valid NHS number")]
    public async Task Run_WhenNhsNumberPassesChecksumValidation_ProceedsWithNormalFlow(
        string caseNumber, string nhsNumber, string forename, string familyName, string dateOfBirth, string bsoCode, string reasonForAdding, string? dummyGpCode)
    {
        // Arrange
        var requestBodyJson = CreateRequestBodyJson(caseNumber, nhsNumber, forename, familyName, dateOfBirth, bsoCode, reasonForAdding, dummyGpCode);
        var requestBodyStream = new MemoryStream(Encoding.UTF8.GetBytes(requestBodyJson));
        _mockHttpRequest.Setup(r => r.Body).Returns(requestBodyStream);
        _mockServiceNowCasesClient.Setup(x => x.Add(It.Is<ServicenowCase>(c =>
                c.ServicenowId == caseNumber &&
                c.NhsNumber == long.Parse(nhsNumber) &&
                c.Status == ServiceNowStatus.New
            ))).ReturnsAsync(true);
        _mockQueueClient.Setup(x => x.AddAsync(It.Is<ServiceNowParticipant>(p =>
                p.ScreeningId == 1 &&
                p.ServiceNowCaseNumber == caseNumber &&
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
        _mockServiceNowClient.VerifyNoOtherCalls();
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
                forename,
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
