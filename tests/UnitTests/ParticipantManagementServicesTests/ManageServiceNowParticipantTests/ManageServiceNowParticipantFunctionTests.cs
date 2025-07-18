namespace NHS.CohortManager.Tests.ParticipantManagementServiceTests;

using System.Net;
using System.Text;
using System.Text.Json;
using Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Model;
using Model.Enums;
using Moq;
using NHS.CohortManager.ParticipantManagementServices;

[TestClass]
public class ManageServiceNowParticipantFunctionTests
{
    private readonly Mock<ILogger<ManageServiceNowParticipantFunction>> _loggerMock = new();
    private readonly Mock<IOptions<ManageServiceNowParticipantConfig>> _configMock = new();
    private readonly Mock<IHttpClientFunction> _httpClientFunctionMock = new();
    private readonly Mock<IExceptionHandler> _handleExceptionMock = new();
    private readonly ServiceNowParticipant _message;
    private readonly ManageServiceNowParticipantFunction _function;

    public ManageServiceNowParticipantFunctionTests()
    {
        _message = new ServiceNowParticipant()
        {
            NhsNumber = "1234567890",
            FirstName = "",
            FamilyName = "",
            DateOfBirth = "1970-01-01",
            ServiceNowRecordNumber = "123"
        };

        var config = new ManageServiceNowParticipantConfig
        {
            RetrievePdsDemographicURL = "http://localhost:8082/api/RetrievePDSDemographic",
            SendServiceNowMessageURL = "http://localhost:9092/api/servicenow/send"
        };
        _configMock.Setup(c => c.Value).Returns(config);

        _function = new ManageServiceNowParticipantFunction(
            _loggerMock.Object,
            _configMock.Object,
            _httpClientFunctionMock.Object,
            _handleExceptionMock.Object
        );
    }

    [TestMethod]
    public async Task Run_WhenNoPdsMatch_SendsServiceNowMessageType1()
    {
        // Arrange
        _httpClientFunctionMock.Setup(x => x.SendGetResponse($"{_configMock.Object.Value.RetrievePdsDemographicURL}?nhsNumber={_message.NhsNumber}"))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NotFound))
            .Verifiable();
        var expectedRequestBody = new SendServiceNowMessageRequestBody
        {
            MessageType = ServiceNowMessageType.UnableToVerifyParticipant
        };
        var expectedRequestBodyJson = JsonSerializer.Serialize(expectedRequestBody);

        // Act
        await _function.Run(_message);

        // Assert
        _httpClientFunctionMock.Verify(x => x.SendPut($"{_configMock.Object.Value.SendServiceNowMessageURL}/{_message.ServiceNowRecordNumber}", expectedRequestBodyJson), Times.Once());
        _httpClientFunctionMock.Verify();
        _httpClientFunctionMock.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task Run_WhenNhsNumberSuperseded_SendsServiceNowMessageType1()
    {
        // Arrange
        var json = JsonSerializer.Serialize(new ParticipantDemographic
        {
            NhsNumber = 123
        });
        _httpClientFunctionMock.Setup(x => x.SendGetResponse($"{_configMock.Object.Value.RetrievePdsDemographicURL}?nhsNumber={_message.NhsNumber}"))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            }).Verifiable();
        var expectedRequestBody = new SendServiceNowMessageRequestBody
        {
            MessageType = ServiceNowMessageType.UnableToVerifyParticipant
        };
        var expectedRequestBodyJson = JsonSerializer.Serialize(expectedRequestBody);

        // Act
        await _function.Run(_message);

        // Assert
        _httpClientFunctionMock.Verify(x => x.SendPut($"{_configMock.Object.Value.SendServiceNowMessageURL}/{_message.ServiceNowRecordNumber}", expectedRequestBodyJson), Times.Once());
        _httpClientFunctionMock.Verify();
        _httpClientFunctionMock.VerifyNoOtherCalls();
    }

    [TestMethod]
    [DataRow(HttpStatusCode.BadRequest)]
    [DataRow(HttpStatusCode.InternalServerError)]
    public async Task Run_WhenPdsReturnsUnexpectedResponse_SendsServiceNowMessageType2AndCreatesSystemExceptionLog(HttpStatusCode httpStatusCode)
    {
        // Arrange
        _httpClientFunctionMock.Setup(x => x.SendGetResponse($"{_configMock.Object.Value.RetrievePdsDemographicURL}?nhsNumber={_message.NhsNumber}"))
            .ReturnsAsync(new HttpResponseMessage(httpStatusCode)).Verifiable();
        var expectedRequestBody = new SendServiceNowMessageRequestBody
        {
            MessageType = ServiceNowMessageType.AddRequestInProgress
        };
        var expectedRequestBodyJson = JsonSerializer.Serialize(expectedRequestBody);

        // Act
        await _function.Run(_message);

        // Assert
        _httpClientFunctionMock.Verify(x => x.SendPut($"{_configMock.Object.Value.SendServiceNowMessageURL}/{_message.ServiceNowRecordNumber}", expectedRequestBodyJson), Times.Once());
        _httpClientFunctionMock.Verify();
        _httpClientFunctionMock.VerifyNoOtherCalls();

        _handleExceptionMock.Verify(x => x.CreateSystemExceptionLog(
            It.Is<Exception>(e => e.Message == $"Request to PDS returned an unexpected response. Status code: {httpStatusCode}"),
            It.Is<ServiceNowParticipant>(p => p.NhsNumber == _message.NhsNumber
                && p.FirstName == _message.FirstName
                && p.FamilyName == _message.FamilyName
                && p.DateOfBirth == _message.DateOfBirth
                && p.ServiceNowRecordNumber == _message.ServiceNowRecordNumber)));
        _handleExceptionMock.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task Run_WhenExceptionOccurs_SendsServiceNowMessageType2AndCreatesSystemExceptionLog()
    {
        // Arrange
        var expectedException = new HttpRequestException();
        _httpClientFunctionMock.Setup(x => x.SendGetResponse($"{_configMock.Object.Value.RetrievePdsDemographicURL}?nhsNumber={_message.NhsNumber}"))
            .ThrowsAsync(expectedException).Verifiable();
        var expectedRequestBody = new SendServiceNowMessageRequestBody
        {
            MessageType = ServiceNowMessageType.AddRequestInProgress
        };
        var expectedRequestBodyJson = JsonSerializer.Serialize(expectedRequestBody);

        // Act
        await _function.Run(_message);

        // Assert
        _httpClientFunctionMock.Verify(x => x.SendPut($"{_configMock.Object.Value.SendServiceNowMessageURL}/{_message.ServiceNowRecordNumber}", expectedRequestBodyJson), Times.Once());
        _httpClientFunctionMock.Verify();
        _httpClientFunctionMock.VerifyNoOtherCalls();

        _handleExceptionMock.Verify(x => x.CreateSystemExceptionLog(
            expectedException,
            It.Is<ServiceNowParticipant>(p => p.NhsNumber == _message.NhsNumber
                && p.FirstName == _message.FirstName
                && p.FamilyName == _message.FamilyName
                && p.DateOfBirth == _message.DateOfBirth
                && p.ServiceNowRecordNumber == _message.ServiceNowRecordNumber)));
        _handleExceptionMock.VerifyNoOtherCalls();
    }
}
