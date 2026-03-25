namespace NHS.CohortManager.Tests.ParticipantManagementServiceTests;

using System.Net;
using System.Text;
using System.Text.Json;
using Common;
using DataServices.Client;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Model;
using Model.Constants;
using Model.Enums;
using Moq;
using NHS.CohortManager.ParticipantManagementServices;

[TestClass]
public class ReceiveRemoveDummyGPCodeFunctionTests
{
    private readonly Mock<ILogger<ReceiveRemoveDummyGpCodeFunction>> _loggerMock = new();
    private readonly CreateResponse _createResponse = new();
    private readonly Mock<IHttpClientFunction> _httpClientFunctionMock = new();
    private readonly Mock<IQueueClient> _queueClientMock = new();
    private readonly Mock<IOptions<RemoveDummyGpCodeConfig>> _configMock = new();
    private readonly Mock<FunctionContext> _contextMock = new();
    private readonly Mock<HttpRequestData> _httpRequestMock;
    private readonly ReceiveRemoveDummyGpCodeFunction _function;

    public ReceiveRemoveDummyGPCodeFunctionTests()
    {
        _configMock.Setup(x => x.Value).Returns(new RemoveDummyGpCodeConfig
        {
            RetrievePdsDemographicURL = "http://localhost:8082/api/RetrievePDSDemographic",
            ServiceBusConnectionString_client_internal = "Endpoint=",
            ServiceNowParticipantManagementTopic = "servicenow-participant-management-topic"
        });

        _function = new ReceiveRemoveDummyGpCodeFunction(
            _loggerMock.Object,
            _createResponse,
            _httpClientFunctionMock.Object,
            _queueClientMock.Object,
            _configMock.Object);

        _httpRequestMock = new Mock<HttpRequestData>(_contextMock.Object);
        _httpRequestMock.Setup(r => r.CreateResponse()).Returns(() =>
        {
            var response = new Mock<HttpResponseData>(_contextMock.Object);
            response.SetupProperty(r => r.Headers, new HttpHeadersCollection());
            response.SetupProperty(r => r.StatusCode);
            response.SetupProperty(r => r.Body, new MemoryStream());
            return response.Object;
        });
    }

    [TestMethod]
    public async Task Run_WhenRequestIsValidAndEnqueueSucceeds_ReturnsAcceptedAndPublishesExpectedParticipant()
    {
        // Arrange
        const string nhsNumber = "9434765919";
        const string requestId = "CS123456789";
        var requestJson = """
                          {
                            "nhs_number": "9434765919",
                            "forename": "Jane",
                            "surname": "Smith",
                            "date_of_birth": "1980-10-22",
                            "request_id": "CS123456789"
                          }
                          """;
        _httpRequestMock.Setup(r => r.Body).Returns(new MemoryStream(Encoding.UTF8.GetBytes(requestJson)));

        var pdsDemographic = new PdsDemographic
        {
            NhsNumber = nhsNumber,
            FirstName = "Jane",
            FamilyName = "Smith",
            DateOfBirth = "1980-10-22",
            CurrentPosting = "ABC"
        };

        _httpClientFunctionMock
            .Setup(x => x.SendGetResponse($"{_configMock.Object.Value.RetrievePdsDemographicURL}?nhsNumber={nhsNumber}"))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(pdsDemographic), Encoding.UTF8, "application/json")
            });

        _queueClientMock
            .Setup(x => x.AddAsync(It.Is<ServiceNowParticipant>(p =>
                    p.ServiceNowCaseNumber == requestId &&
                    p.ScreeningId == 1 &&
                    p.NhsNumber == long.Parse(nhsNumber) &&
                    p.FirstName == "Jane" &&
                    p.FamilyName == "Smith" &&
                    p.DateOfBirth == new DateOnly(1980, 10, 22) &&
                    p.BsoCode == "ABC" &&
                    p.ReasonForAdding == ServiceNowReasonsForAdding.DummyGpCodeRemoval &&
                    p.RequiredGpCode == null),
                _configMock.Object.Value.ServiceNowParticipantManagementTopic))
            .ReturnsAsync(true)
            .Verifiable();

        // Act
        var result = await _function.Run(_httpRequestMock.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.Accepted, result.StatusCode);
        _queueClientMock.Verify();
    }

    [TestMethod]
    public async Task Run_WhenNhsNumberIsInvalid_ReturnsBadRequestWithInvalidNhsNumberMessage()
    {
        // Arrange
        var requestJson = """
                          {
                            "nhs_number": "1234567891",
                            "forename": "Jane",
                            "surname": "Smith",
                            "date_of_birth": "1980-10-22",
                            "request_id": "CS123456789"
                          }
                          """;
        _httpRequestMock.Setup(r => r.Body).Returns(new MemoryStream(Encoding.UTF8.GetBytes(requestJson)));

        // Act
        var result = await _function.Run(_httpRequestMock.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
        using var reader = new StreamReader(result.Body);
        var body = await reader.ReadToEndAsync();
        Assert.AreEqual("Invalid NHS Number", body);
        _httpClientFunctionMock.VerifyNoOtherCalls();
        _queueClientMock.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task Run_WhenPdsRecordIsNotFound_ReturnsBadRequestWithPatientNotFoundMessage()
    {
        // Arrange
        const string nhsNumber = "9434765919";
        var requestJson = """
                          {
                            "nhs_number": "9434765919",
                            "forename": "Jane",
                            "surname": "Smith",
                            "date_of_birth": "1980-10-22",
                            "request_id": "CS123456789"
                          }
                          """;
        _httpRequestMock.Setup(r => r.Body).Returns(new MemoryStream(Encoding.UTF8.GetBytes(requestJson)));

        _httpClientFunctionMock
            .Setup(x => x.SendGetResponse($"{_configMock.Object.Value.RetrievePdsDemographicURL}?nhsNumber={nhsNumber}"))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NotFound));

        // Act
        var result = await _function.Run(_httpRequestMock.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
        using var reader = new StreamReader(result.Body);
        var body = await reader.ReadToEndAsync();
        Assert.AreEqual("Patient not found", body);
        _queueClientMock.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task Run_WhenPdsDemographicDoesNotMatchRequest_ReturnsBadRequestWithPatientNotFoundMessage()
    {
        // Arrange
        const string nhsNumber = "9434765919";
        var requestJson = """
                          {
                            "nhs_number": "9434765919",
                            "forename": "Jane",
                            "surname": "Smith",
                            "date_of_birth": "1980-10-22",
                            "request_id": "CS123456789"
                          }
                          """;
        _httpRequestMock.Setup(r => r.Body).Returns(new MemoryStream(Encoding.UTF8.GetBytes(requestJson)));

        var pdsDemographic = new PdsDemographic
        {
            NhsNumber = nhsNumber,
            FirstName = "Janet",
            FamilyName = "Smith",
            DateOfBirth = "1980-10-22"
        };

        _httpClientFunctionMock
            .Setup(x => x.SendGetResponse($"{_configMock.Object.Value.RetrievePdsDemographicURL}?nhsNumber={nhsNumber}"))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(pdsDemographic), Encoding.UTF8, "application/json")
            });

        // Act
        var result = await _function.Run(_httpRequestMock.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
        using var reader = new StreamReader(result.Body);
        var body = await reader.ReadToEndAsync();
        Assert.AreEqual("Patient not found", body);
        _queueClientMock.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task Run_WhenEnqueueFails_ReturnsInternalServerError()
    {
        // Arrange
        const string nhsNumber = "9434765919";
        var requestJson = """
                          {
                            "nhs_number": "9434765919",
                            "forename": "Jane",
                            "surname": "Smith",
                            "date_of_birth": "1980-10-22",
                            "request_id": "CS123456789"
                          }
                          """;
        _httpRequestMock.Setup(r => r.Body).Returns(new MemoryStream(Encoding.UTF8.GetBytes(requestJson)));

        var pdsDemographic = new PdsDemographic
        {
            NhsNumber = nhsNumber,
            FirstName = "Jane",
            FamilyName = "Smith",
            DateOfBirth = "1980-10-22",
            CurrentPosting = "ABC"
        };

        _httpClientFunctionMock
            .Setup(x => x.SendGetResponse($"{_configMock.Object.Value.RetrievePdsDemographicURL}?nhsNumber={nhsNumber}"))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(pdsDemographic), Encoding.UTF8, "application/json")
            });

        _queueClientMock
            .Setup(x => x.AddAsync(It.IsAny<ServiceNowParticipant>(), _configMock.Object.Value.ServiceNowParticipantManagementTopic))
            .ReturnsAsync(false);

        // Act
        var result = await _function.Run(_httpRequestMock.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_WhenRequestBodyIsMalformedJson_ReturnsBadRequestWithPatientNotFoundMessage()
    {
        // Arrange
        _httpRequestMock.Setup(r => r.Body).Returns(new MemoryStream(Encoding.UTF8.GetBytes("{")));

        // Act
        var result = await _function.Run(_httpRequestMock.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
        using var reader = new StreamReader(result.Body);
        var body = await reader.ReadToEndAsync();
        Assert.AreEqual("Patient not found", body);
        _httpClientFunctionMock.VerifyNoOtherCalls();
        _queueClientMock.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task Run_WhenRequestValidationFails_ReturnsBadRequestWithPatientNotFoundMessage()
    {
        // Arrange
        var requestJson = """
                          {
                            "nhs_number": "9434765919",
                            "forename": "",
                            "surname": "Smith",
                            "date_of_birth": "1980-10-22",
                            "request_id": "CS123456789"
                          }
                          """;
        _httpRequestMock.Setup(r => r.Body).Returns(new MemoryStream(Encoding.UTF8.GetBytes(requestJson)));

        // Act
        var result = await _function.Run(_httpRequestMock.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
        using var reader = new StreamReader(result.Body);
        var body = await reader.ReadToEndAsync();
        Assert.AreEqual("Patient not found", body);
        _httpClientFunctionMock.VerifyNoOtherCalls();
        _queueClientMock.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task Run_WhenPdsResponseStatusIsUnexpected_ReturnsInternalServerError()
    {
        // Arrange
        const string nhsNumber = "9434765919";
        var requestJson = """
                          {
                            "nhs_number": "9434765919",
                            "forename": "Jane",
                            "surname": "Smith",
                            "date_of_birth": "1980-10-22",
                            "request_id": "CS123456789"
                          }
                          """;
        _httpRequestMock.Setup(r => r.Body).Returns(new MemoryStream(Encoding.UTF8.GetBytes(requestJson)));

        _httpClientFunctionMock
            .Setup(x => x.SendGetResponse($"{_configMock.Object.Value.RetrievePdsDemographicURL}?nhsNumber={nhsNumber}"))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadGateway));

        // Act
        var result = await _function.Run(_httpRequestMock.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
        _queueClientMock.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task Run_WhenPdsDemographicResponseIsNull_ReturnsInternalServerError()
    {
        // Arrange
        const string nhsNumber = "9434765919";
        var requestJson = """
                          {
                            "nhs_number": "9434765919",
                            "forename": "Jane",
                            "surname": "Smith",
                            "date_of_birth": "1980-10-22",
                            "request_id": "CS123456789"
                          }
                          """;
        _httpRequestMock.Setup(r => r.Body).Returns(new MemoryStream(Encoding.UTF8.GetBytes(requestJson)));

        _httpClientFunctionMock
            .Setup(x => x.SendGetResponse($"{_configMock.Object.Value.RetrievePdsDemographicURL}?nhsNumber={nhsNumber}"))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("null", Encoding.UTF8, "application/json")
            });

        // Act
        var result = await _function.Run(_httpRequestMock.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
        _queueClientMock.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task Run_WhenPdsPayloadIsMalformedJson_ReturnsBadRequestWithPatientNotFoundMessage()
    {
        // Arrange
        const string nhsNumber = "9434765919";
        var requestJson = """
                          {
                            "nhs_number": "9434765919",
                            "forename": "Jane",
                            "surname": "Smith",
                            "date_of_birth": "1980-10-22",
                            "request_id": "CS123456789"
                          }
                          """;
        _httpRequestMock.Setup(r => r.Body).Returns(new MemoryStream(Encoding.UTF8.GetBytes(requestJson)));

        _httpClientFunctionMock
            .Setup(x => x.SendGetResponse($"{_configMock.Object.Value.RetrievePdsDemographicURL}?nhsNumber={nhsNumber}"))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{", Encoding.UTF8, "application/json")
            });

        // Act
        var result = await _function.Run(_httpRequestMock.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
        using var reader = new StreamReader(result.Body);
        var body = await reader.ReadToEndAsync();
        Assert.AreEqual("Patient not found", body);
        _queueClientMock.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task Run_WhenHttpClientThrowsUnexpectedException_ReturnsInternalServerError()
    {
        // Arrange
        const string nhsNumber = "9434765919";
        var requestJson = """
                          {
                            "nhs_number": "9434765919",
                            "forename": "Jane",
                            "surname": "Smith",
                            "date_of_birth": "1980-10-22",
                            "request_id": "CS123456789"
                          }
                          """;
        _httpRequestMock.Setup(r => r.Body).Returns(new MemoryStream(Encoding.UTF8.GetBytes(requestJson)));

        _httpClientFunctionMock
            .Setup(x => x.SendGetResponse($"{_configMock.Object.Value.RetrievePdsDemographicURL}?nhsNumber={nhsNumber}"))
            .ThrowsAsync(new InvalidOperationException("Unexpected failure"));

        // Act
        var result = await _function.Run(_httpRequestMock.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
        _queueClientMock.VerifyNoOtherCalls();
    }
}
