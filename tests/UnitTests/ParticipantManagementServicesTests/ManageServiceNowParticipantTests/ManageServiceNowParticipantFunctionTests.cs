namespace NHS.CohortManager.Tests.ParticipantManagementServiceTests;

using System.Linq.Expressions;
using System.Net;
using System.Text;
using System.Text.Json;
using Common;
using DataServices.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Model;
using Model.Enums;
using Moq;
using NHS.CohortManager.ParticipantManagementServices;
using NHS.CohortManager.Tests.TestUtils;

[TestClass]
public class ManageServiceNowParticipantFunctionTests
{
    private readonly Mock<ILogger<ManageServiceNowParticipantFunction>> _loggerMock = new();
    private readonly Mock<IOptions<ManageServiceNowParticipantConfig>> _configMock = new();
    private readonly Mock<IHttpClientFunction> _httpClientFunctionMock = new();
    private readonly Mock<IExceptionHandler> _handleExceptionMock = new();
    private readonly Mock<IDataServiceClient<ParticipantManagement>> _dataServiceClientMock = new();
    private readonly ServiceNowParticipant _serviceNowParticipant;
    private readonly ManageServiceNowParticipantFunction _function;

    private readonly string _messageType1Request;
    private readonly string _messageType2Request;

    public ManageServiceNowParticipantFunctionTests()
    {
        _serviceNowParticipant = new ServiceNowParticipant()
        {
            ScreeningId = 1,
            NhsNumber = 1234567890,
            FirstName = "Samantha",
            FamilyName = "Bloggs",
            DateOfBirth = new DateOnly(1970, 1, 1),
            ServiceNowRecordNumber = "CS123",
            BsoCode = "ABC",
            ReasonForAdding = ServiceNowReasonsForAdding.RequiresCeasing
        };

        var config = new ManageServiceNowParticipantConfig
        {
            RetrievePdsDemographicURL = "http://localhost:8082/api/RetrievePDSDemographic",
            SendServiceNowMessageURL = "http://localhost:9092/api/servicenow/send",
            ParticipantManagementURL = "http://localhost:7994/api/ParticipantManagementDataService"
        };
        _configMock.Setup(c => c.Value).Returns(config);

        _function = new ManageServiceNowParticipantFunction(
            _loggerMock.Object,
            _configMock.Object,
            _httpClientFunctionMock.Object,
            _handleExceptionMock.Object,
            _dataServiceClientMock.Object
        );

        _messageType1Request = JsonSerializer.Serialize(new SendServiceNowMessageRequestBody
        {
            MessageType = ServiceNowMessageType.UnableToAddParticipant
        });
        _messageType2Request = JsonSerializer.Serialize(new SendServiceNowMessageRequestBody
        {
            MessageType = ServiceNowMessageType.AddRequestInProgress
        });
    }

    [TestMethod]
    public async Task Run_WhenNoPdsMatch_SendsServiceNowMessageType1()
    {
        // Arrange
        _httpClientFunctionMock.Setup(x => x.SendGetResponse($"{_configMock.Object.Value.RetrievePdsDemographicURL}?nhsNumber={_serviceNowParticipant.NhsNumber}"))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NotFound)).Verifiable();

        // Act
        await _function.Run(_serviceNowParticipant);

        // Assert
        _httpClientFunctionMock.Verify(x => x.SendPut($"{_configMock.Object.Value.SendServiceNowMessageURL}/{_serviceNowParticipant.ServiceNowRecordNumber}", _messageType1Request), Times.Once());
        _httpClientFunctionMock.Verify();
        _httpClientFunctionMock.VerifyNoOtherCalls();

        _handleExceptionMock.Verify(x => x.CreateSystemExceptionLog(
                It.Is<Exception>(e => e.Message == "Request to PDS for ServiceNow Participant returned a 404 NotFound response."),
                It.Is<ServiceNowParticipant>(p => p.NhsNumber == _serviceNowParticipant.NhsNumber
                && p.FirstName == _serviceNowParticipant.FirstName
                && p.FamilyName == _serviceNowParticipant.FamilyName
                && p.DateOfBirth == _serviceNowParticipant.DateOfBirth
                && p.ServiceNowRecordNumber == _serviceNowParticipant.ServiceNowRecordNumber)), Times.Once);
        _handleExceptionMock.VerifyNoOtherCalls();

        _dataServiceClientMock.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task Run_WhenNhsNumberSuperseded_SendsServiceNowMessageType1()
    {
        // Arrange
        var json = JsonSerializer.Serialize(new ParticipantDemographic
        {
            NhsNumber = 123
        });
        _httpClientFunctionMock.Setup(x => x.SendGetResponse($"{_configMock.Object.Value.RetrievePdsDemographicURL}?nhsNumber={_serviceNowParticipant.NhsNumber}"))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            }).Verifiable();

        // Act
        await _function.Run(_serviceNowParticipant);

        // Assert
        _httpClientFunctionMock.Verify(x => x.SendPut($"{_configMock.Object.Value.SendServiceNowMessageURL}/{_serviceNowParticipant.ServiceNowRecordNumber}", _messageType1Request), Times.Once());
        _httpClientFunctionMock.Verify();
        _httpClientFunctionMock.VerifyNoOtherCalls();

        _handleExceptionMock.Verify(x => x.CreateSystemExceptionLog(
            It.Is<Exception>(e => e.Message == "NHS Numbers don't match for ServiceNow Participant and PDS, NHS Number must have been superseded"),
            It.Is<ServiceNowParticipant>(p => p.NhsNumber == _serviceNowParticipant.NhsNumber
                && p.FirstName == _serviceNowParticipant.FirstName
                && p.FamilyName == _serviceNowParticipant.FamilyName
                && p.DateOfBirth == _serviceNowParticipant.DateOfBirth
                && p.ServiceNowRecordNumber == _serviceNowParticipant.ServiceNowRecordNumber)), Times.Once);
        _handleExceptionMock.VerifyNoOtherCalls();

        _dataServiceClientMock.VerifyNoOtherCalls();
    }

    [TestMethod]
    [DataRow(HttpStatusCode.BadRequest)]
    [DataRow(HttpStatusCode.InternalServerError)]
    public async Task Run_WhenPdsReturnsUnexpectedResponse_SendsServiceNowMessageType2AndCreatesSystemExceptionLog(HttpStatusCode httpStatusCode)
    {
        // Arrange
        _httpClientFunctionMock.Setup(x => x.SendGetResponse($"{_configMock.Object.Value.RetrievePdsDemographicURL}?nhsNumber={_serviceNowParticipant.NhsNumber}"))
            .ReturnsAsync(new HttpResponseMessage(httpStatusCode)).Verifiable();

        // Act
        await _function.Run(_serviceNowParticipant);

        // Assert
        _httpClientFunctionMock.Verify(x => x.SendPut($"{_configMock.Object.Value.SendServiceNowMessageURL}/{_serviceNowParticipant.ServiceNowRecordNumber}", _messageType2Request), Times.Once());
        _httpClientFunctionMock.Verify();
        _httpClientFunctionMock.VerifyNoOtherCalls();

        _handleExceptionMock.Verify(x => x.CreateSystemExceptionLog(
            It.Is<Exception>(e => e.Message == $"Request to PDS for ServiceNow Participant returned an unexpected response. Status code: {httpStatusCode}"),
            It.Is<ServiceNowParticipant>(p => p.NhsNumber == _serviceNowParticipant.NhsNumber
                && p.FirstName == _serviceNowParticipant.FirstName
                && p.FamilyName == _serviceNowParticipant.FamilyName
                && p.DateOfBirth == _serviceNowParticipant.DateOfBirth
                && p.ServiceNowRecordNumber == _serviceNowParticipant.ServiceNowRecordNumber)), Times.Once);
        _handleExceptionMock.VerifyNoOtherCalls();

        _dataServiceClientMock.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task Run_WhenExceptionOccursSendingPdsRequest_SendsServiceNowMessageType2AndCreatesSystemExceptionLog()
    {
        // Arrange
        var expectedException = new HttpRequestException();
        _httpClientFunctionMock.Setup(x => x.SendGetResponse($"{_configMock.Object.Value.RetrievePdsDemographicURL}?nhsNumber={_serviceNowParticipant.NhsNumber}"))
            .ThrowsAsync(expectedException).Verifiable();

        // Act
        await _function.Run(_serviceNowParticipant);

        // Assert
        _httpClientFunctionMock.Verify(x => x.SendPut($"{_configMock.Object.Value.SendServiceNowMessageURL}/{_serviceNowParticipant.ServiceNowRecordNumber}", _messageType2Request), Times.Once());
        _httpClientFunctionMock.Verify();
        _httpClientFunctionMock.VerifyNoOtherCalls();

        _handleExceptionMock.Verify(x => x.CreateSystemExceptionLog(
            expectedException,
            It.Is<ServiceNowParticipant>(p => p.NhsNumber == _serviceNowParticipant.NhsNumber
                && p.FirstName == _serviceNowParticipant.FirstName
                && p.FamilyName == _serviceNowParticipant.FamilyName
                && p.DateOfBirth == _serviceNowParticipant.DateOfBirth
                && p.ServiceNowRecordNumber == _serviceNowParticipant.ServiceNowRecordNumber)));
        _handleExceptionMock.VerifyNoOtherCalls();

        _dataServiceClientMock.VerifyNoOtherCalls();
    }

    [TestMethod]
    [DataRow("Sam", "Bloggs", "1970-01-01")]        // First names don't match
    [DataRow("Samantha", "bloggs", "1970-01-01")]   // Family names don't match
    [DataRow("Samantha", "Bloggs", "1970-01-02")]   // Dates of birth don't match
    public async Task Run_WhenParticipantDataDoesNotMatchPdsData_SendsServiceNowMessageType1(
        string firstName, string familyName, string dateOfBirth)
    {
        // Arrange
        var json = JsonSerializer.Serialize(new ParticipantDemographic
        {
            NhsNumber = _serviceNowParticipant.NhsNumber,
            GivenName = firstName,
            FamilyName = familyName,
            DateOfBirth = dateOfBirth
        });
        _httpClientFunctionMock.Setup(x => x.SendGetResponse($"{_configMock.Object.Value.RetrievePdsDemographicURL}?nhsNumber={_serviceNowParticipant.NhsNumber}"))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            }).Verifiable();

        // Act
        await _function.Run(_serviceNowParticipant);

        // Assert
        _httpClientFunctionMock.Verify(x => x.SendPut($"{_configMock.Object.Value.SendServiceNowMessageURL}/{_serviceNowParticipant.ServiceNowRecordNumber}", _messageType1Request), Times.Once());
        _httpClientFunctionMock.Verify();
        _httpClientFunctionMock.VerifyNoOtherCalls();

        _handleExceptionMock.Verify(x => x.CreateSystemExceptionLog(
            It.Is<Exception>(e => e.Message == "Participant data from ServiceNow does not match participant data from PDS"),
            It.Is<ServiceNowParticipant>(p => p.NhsNumber == _serviceNowParticipant.NhsNumber
                && p.FirstName == _serviceNowParticipant.FirstName
                && p.FamilyName == _serviceNowParticipant.FamilyName
                && p.DateOfBirth == _serviceNowParticipant.DateOfBirth
                && p.ServiceNowRecordNumber == _serviceNowParticipant.ServiceNowRecordNumber)), Times.Once);
        _handleExceptionMock.VerifyNoOtherCalls();

        _dataServiceClientMock.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task Run_WhenServiceNowParticipantIsValidAndDoesNotExistInTheDataStore_AddsTheNewParticipant()
    {
        // Arrange
        var json = JsonSerializer.Serialize(new ParticipantDemographic
        {
            NhsNumber = _serviceNowParticipant.NhsNumber,
            GivenName = _serviceNowParticipant.FirstName,
            FamilyName = _serviceNowParticipant.FamilyName,
            DateOfBirth = _serviceNowParticipant.DateOfBirth.ToString("yyyy-MM-dd")
        });
        _httpClientFunctionMock.Setup(x => x.SendGetResponse($"{_configMock.Object.Value.RetrievePdsDemographicURL}?nhsNumber={_serviceNowParticipant.NhsNumber}"))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            }).Verifiable();

        _dataServiceClientMock.Setup(client => client.GetSingleByFilter(
            It.Is<Expression<Func<ParticipantManagement, bool>>>(x =>
                x.Compile().Invoke(new ParticipantManagement { NHSNumber = _serviceNowParticipant.NhsNumber, ScreeningId = _serviceNowParticipant.ScreeningId })
            ))).ReturnsAsync((ParticipantManagement)null!).Verifiable();

        _dataServiceClientMock.Setup(x => x.Add(It.Is<ParticipantManagement>(p =>
                p.ScreeningId == _serviceNowParticipant.ScreeningId &&
                p.NHSNumber == _serviceNowParticipant.NhsNumber &&
                p.RecordType == Actions.New &&
                p.EligibilityFlag == 1 &&
                p.ReferralFlag == 1 &&
                p.RecordInsertDateTime != null)))
            .ReturnsAsync(true).Verifiable();

        // Act
        await _function.Run(_serviceNowParticipant);

        // Assert
        _httpClientFunctionMock.Verify();
        _httpClientFunctionMock.VerifyNoOtherCalls();

        _handleExceptionMock.VerifyNoOtherCalls();

        _dataServiceClientMock.Verify();
        _dataServiceClientMock.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task Run_WhenServiceNowParticipantIsValidAndExistsInTheDataStoreButIsBlocked_DoesNotUpdateTheParticipantAndSendsMessageType1()
    {
        // Arrange
        var json = JsonSerializer.Serialize(new ParticipantDemographic
        {
            NhsNumber = _serviceNowParticipant.NhsNumber,
            GivenName = _serviceNowParticipant.FirstName,
            FamilyName = _serviceNowParticipant.FamilyName,
            DateOfBirth = _serviceNowParticipant.DateOfBirth.ToString("yyyy-MM-dd")
        });
        _httpClientFunctionMock.Setup(x => x.SendGetResponse($"{_configMock.Object.Value.RetrievePdsDemographicURL}?nhsNumber={_serviceNowParticipant.NhsNumber}"))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            }).Verifiable();

        _dataServiceClientMock.Setup(client => client.GetSingleByFilter(
            It.Is<Expression<Func<ParticipantManagement, bool>>>(x =>
                x.Compile().Invoke(new ParticipantManagement { NHSNumber = _serviceNowParticipant.NhsNumber, ScreeningId = _serviceNowParticipant.ScreeningId })
            ))).ReturnsAsync(new ParticipantManagement { ParticipantId = 123, BlockedFlag = 1 }).Verifiable();

        // Act
        await _function.Run(_serviceNowParticipant);

        // Assert
        _httpClientFunctionMock.Verify(x => x.SendPut($"{_configMock.Object.Value.SendServiceNowMessageURL}/{_serviceNowParticipant.ServiceNowRecordNumber}", _messageType1Request), Times.Once());
        _httpClientFunctionMock.Verify();
        _httpClientFunctionMock.VerifyNoOtherCalls();

        _handleExceptionMock.Verify(x => x.CreateSystemExceptionLog(
            It.Is<Exception>(e => e.Message == "Participant data from ServiceNow is blocked"),
            It.Is<ServiceNowParticipant>(p => p.NhsNumber == _serviceNowParticipant.NhsNumber
                && p.FirstName == _serviceNowParticipant.FirstName
                && p.FamilyName == _serviceNowParticipant.FamilyName
                && p.DateOfBirth == _serviceNowParticipant.DateOfBirth
                && p.ServiceNowRecordNumber == _serviceNowParticipant.ServiceNowRecordNumber)), Times.Once);
        _handleExceptionMock.VerifyNoOtherCalls();

        _dataServiceClientMock.Verify();
        _dataServiceClientMock.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task Run_WhenServiceNowParticipantIsValidAndExistsInTheDataStoreAndIsNotBlocked_UpdatesTheParticipant()
    {
        // Arrange
        var json = JsonSerializer.Serialize(new ParticipantDemographic
        {
            NhsNumber = _serviceNowParticipant.NhsNumber,
            GivenName = _serviceNowParticipant.FirstName,
            FamilyName = _serviceNowParticipant.FamilyName,
            DateOfBirth = _serviceNowParticipant.DateOfBirth.ToString("yyyy-MM-dd")
        });
        _httpClientFunctionMock.Setup(x => x.SendGetResponse($"{_configMock.Object.Value.RetrievePdsDemographicURL}?nhsNumber={_serviceNowParticipant.NhsNumber}"))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            }).Verifiable();

        _dataServiceClientMock.Setup(client => client.GetSingleByFilter(
            It.Is<Expression<Func<ParticipantManagement, bool>>>(x =>
                x.Compile().Invoke(new ParticipantManagement { NHSNumber = _serviceNowParticipant.NhsNumber, ScreeningId = _serviceNowParticipant.ScreeningId })
            ))).ReturnsAsync(new ParticipantManagement { ParticipantId = 123, ScreeningId = _serviceNowParticipant.ScreeningId, NHSNumber = _serviceNowParticipant.NhsNumber }).Verifiable();

        _dataServiceClientMock.Setup(x => x.Update(It.Is<ParticipantManagement>(p =>
                p.ParticipantId == 123 &&
                p.ScreeningId == _serviceNowParticipant.ScreeningId &&
                p.NHSNumber == _serviceNowParticipant.NhsNumber &&
                p.RecordType == Actions.Amended &&
                p.EligibilityFlag == 1 &&
                p.ReferralFlag == 1 &&
                p.RecordUpdateDateTime != null)))
            .ReturnsAsync(true).Verifiable();

        // Act
        await _function.Run(_serviceNowParticipant);

        // Assert
        _httpClientFunctionMock.Verify();
        _httpClientFunctionMock.VerifyNoOtherCalls();

        _handleExceptionMock.VerifyNoOtherCalls();

        _dataServiceClientMock.Verify();
        _dataServiceClientMock.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task Run_WhenServiceNowParticipantIsVhrAndDoesNotExistInDataStore_AddsNewParticipantWithVhrFlag()
    {
        // Arrange
        var vhrParticipant = new ServiceNowParticipant()
        {
            ScreeningId = 1,
            NhsNumber = 1234567890,
            FirstName = "Samantha",
            FamilyName = "Bloggs",
            DateOfBirth = new DateOnly(1970, 1, 1),
            ServiceNowRecordNumber = "CS123",
            BsoCode = "ABC",
            ReasonForAdding = ServiceNowReasonsForAdding.VeryHighRisk
        };

        var json = JsonSerializer.Serialize(new ParticipantDemographic
        {
            NhsNumber = vhrParticipant.NhsNumber,
            GivenName = vhrParticipant.FirstName,
            FamilyName = vhrParticipant.FamilyName,
            DateOfBirth = vhrParticipant.DateOfBirth.ToString("yyyy-MM-dd")
        });

        _httpClientFunctionMock.Setup(x => x.SendGetResponse($"{_configMock.Object.Value.RetrievePdsDemographicURL}?nhsNumber={vhrParticipant.NhsNumber}"))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            }).Verifiable();

        _dataServiceClientMock.Setup(client => client.GetSingleByFilter(
            It.Is<Expression<Func<ParticipantManagement, bool>>>(x =>
                x.Compile().Invoke(new ParticipantManagement { NHSNumber = vhrParticipant.NhsNumber, ScreeningId = vhrParticipant.ScreeningId })
            ))).ReturnsAsync((ParticipantManagement)null!).Verifiable();

        _dataServiceClientMock.Setup(x => x.Add(It.Is<ParticipantManagement>(p =>
                p.ScreeningId == vhrParticipant.ScreeningId &&
                p.NHSNumber == vhrParticipant.NhsNumber &&
                p.RecordType == Actions.New &&
                p.EligibilityFlag == 1 &&
                p.ReferralFlag == 1 &&
                p.IsHigherRisk == 1 &&
                p.RecordInsertDateTime != null)))
            .ReturnsAsync(true).Verifiable();

        // Act
        await _function.Run(vhrParticipant);

        // Assert
        _httpClientFunctionMock.Verify();
        _httpClientFunctionMock.VerifyNoOtherCalls();

        _handleExceptionMock.VerifyNoOtherCalls();

        _dataServiceClientMock.Verify();
        _dataServiceClientMock.VerifyNoOtherCalls();

        _loggerMock.VerifyLogger(LogLevel.Information, "Participant not in participant management table, adding new record");
        _loggerMock.VerifyLogger(LogLevel.Information, $"Participant with NHS Number: {vhrParticipant.NhsNumber} set as High Risk");
    }

    [TestMethod]
    public async Task Run_WhenServiceNowParticipantIsNotVhrAndDoesNotExistInDataStore_AddsNewParticipantWithoutVhrFlag()
    {
        // Arrange
        var json = JsonSerializer.Serialize(new ParticipantDemographic
        {
            NhsNumber = _serviceNowParticipant.NhsNumber,
            GivenName = _serviceNowParticipant.FirstName,
            FamilyName = _serviceNowParticipant.FamilyName,
            DateOfBirth = _serviceNowParticipant.DateOfBirth.ToString("yyyy-MM-dd")
        });

        _httpClientFunctionMock.Setup(x => x.SendGetResponse($"{_configMock.Object.Value.RetrievePdsDemographicURL}?nhsNumber={_serviceNowParticipant.NhsNumber}"))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            }).Verifiable();

        _dataServiceClientMock.Setup(client => client.GetSingleByFilter(
            It.Is<Expression<Func<ParticipantManagement, bool>>>(x =>
                x.Compile().Invoke(new ParticipantManagement { NHSNumber = _serviceNowParticipant.NhsNumber, ScreeningId = _serviceNowParticipant.ScreeningId })
            ))).ReturnsAsync((ParticipantManagement)null!).Verifiable();

        _dataServiceClientMock.Setup(x => x.Add(It.Is<ParticipantManagement>(p =>
                p.ScreeningId == _serviceNowParticipant.ScreeningId &&
                p.NHSNumber == _serviceNowParticipant.NhsNumber &&
                p.RecordType == Actions.New &&
                p.EligibilityFlag == 1 &&
                p.ReferralFlag == 1 &&
                p.IsHigherRisk == null &&
                p.RecordInsertDateTime != null)))
            .ReturnsAsync(true).Verifiable();

        // Act
        await _function.Run(_serviceNowParticipant);

        // Assert
        _httpClientFunctionMock.Verify();
        _httpClientFunctionMock.VerifyNoOtherCalls();

        _handleExceptionMock.VerifyNoOtherCalls();

        _dataServiceClientMock.Verify();
        _dataServiceClientMock.VerifyNoOtherCalls();

        _loggerMock.VerifyLogger(LogLevel.Information, "Participant not in participant management table, adding new record");
    }

    [TestMethod]
    public async Task Run_WhenVhrParticipantExistsWithNullVhrFlag_SetsVhrFlagToTrue()
    {
        // Arrange
        var vhrParticipant = new ServiceNowParticipant()
        {
            ScreeningId = 1,
            NhsNumber = 1234567890,
            FirstName = "Samantha",
            FamilyName = "Bloggs",
            DateOfBirth = new DateOnly(1970, 1, 1),
            ServiceNowRecordNumber = "CS123",
            BsoCode = "ABC",
            ReasonForAdding = ServiceNowReasonsForAdding.VeryHighRisk
        };

        var json = JsonSerializer.Serialize(new ParticipantDemographic
        {
            NhsNumber = vhrParticipant.NhsNumber,
            GivenName = vhrParticipant.FirstName,
            FamilyName = vhrParticipant.FamilyName,
            DateOfBirth = vhrParticipant.DateOfBirth.ToString("yyyy-MM-dd")
        });

        var existingParticipant = new ParticipantManagement
        {
            ParticipantId = 123,
            ScreeningId = vhrParticipant.ScreeningId,
            NHSNumber = vhrParticipant.NhsNumber,
            IsHigherRisk = null
        };

        _httpClientFunctionMock.Setup(x => x.SendGetResponse($"{_configMock.Object.Value.RetrievePdsDemographicURL}?nhsNumber={vhrParticipant.NhsNumber}"))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            }).Verifiable();

        _dataServiceClientMock.Setup(client => client.GetSingleByFilter(
            It.Is<Expression<Func<ParticipantManagement, bool>>>(x =>
                x.Compile().Invoke(new ParticipantManagement { NHSNumber = vhrParticipant.NhsNumber, ScreeningId = vhrParticipant.ScreeningId })
            ))).ReturnsAsync(existingParticipant).Verifiable();

        _dataServiceClientMock.Setup(x => x.Update(It.Is<ParticipantManagement>(p =>
                p.ParticipantId == 123 &&
                p.ScreeningId == vhrParticipant.ScreeningId &&
                p.NHSNumber == vhrParticipant.NhsNumber &&
                p.RecordType == Actions.Amended &&
                p.EligibilityFlag == 1 &&
                p.ReferralFlag == 1 &&
                p.IsHigherRisk == 1 &&
                p.RecordUpdateDateTime != null)))
            .ReturnsAsync(true).Verifiable();

        // Act
        await _function.Run(vhrParticipant);

        // Assert
        _httpClientFunctionMock.Verify();
        _httpClientFunctionMock.VerifyNoOtherCalls();

        _handleExceptionMock.VerifyNoOtherCalls();

        _dataServiceClientMock.Verify();
        _dataServiceClientMock.VerifyNoOtherCalls();

        _loggerMock.VerifyLogger(LogLevel.Information, "Existing participant management record found, updating record 123");
        _loggerMock.VerifyLogger(LogLevel.Information, "Participant 123 set as High Risk based on ServiceNow attributes");
        _loggerMock.VerifyLogger(LogLevel.Information, "Participant 123 still maintained as High Risk");
    }

    [TestMethod]
    public async Task Run_WhenParticipantExistsWithVhrFlagAlreadySet_MaintainsVhrFlag()
    {
        // Arrange
        var json = JsonSerializer.Serialize(new ParticipantDemographic
        {
            NhsNumber = _serviceNowParticipant.NhsNumber,
            GivenName = _serviceNowParticipant.FirstName,
            FamilyName = _serviceNowParticipant.FamilyName,
            DateOfBirth = _serviceNowParticipant.DateOfBirth.ToString("yyyy-MM-dd")
        });

        var existingParticipant = new ParticipantManagement
        {
            ParticipantId = 123,
            ScreeningId = _serviceNowParticipant.ScreeningId,
            NHSNumber = _serviceNowParticipant.NhsNumber,
            IsHigherRisk = 1
        };

        _httpClientFunctionMock.Setup(x => x.SendGetResponse($"{_configMock.Object.Value.RetrievePdsDemographicURL}?nhsNumber={_serviceNowParticipant.NhsNumber}"))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            }).Verifiable();

        _dataServiceClientMock.Setup(client => client.GetSingleByFilter(
            It.Is<Expression<Func<ParticipantManagement, bool>>>(x =>
                x.Compile().Invoke(new ParticipantManagement { NHSNumber = _serviceNowParticipant.NhsNumber, ScreeningId = _serviceNowParticipant.ScreeningId })
            ))).ReturnsAsync(existingParticipant).Verifiable();

        _dataServiceClientMock.Setup(x => x.Update(It.Is<ParticipantManagement>(p =>
                p.ParticipantId == 123 &&
                p.ScreeningId == _serviceNowParticipant.ScreeningId &&
                p.NHSNumber == _serviceNowParticipant.NhsNumber &&
                p.RecordType == Actions.Amended &&
                p.EligibilityFlag == 1 &&
                p.ReferralFlag == 1 &&
                p.IsHigherRisk == 1 &&
                p.RecordUpdateDateTime != null)))
            .ReturnsAsync(true).Verifiable();

        // Act
        await _function.Run(_serviceNowParticipant);

        // Assert
        _httpClientFunctionMock.Verify();
        _httpClientFunctionMock.VerifyNoOtherCalls();

        _handleExceptionMock.VerifyNoOtherCalls();

        _dataServiceClientMock.Verify();
        _dataServiceClientMock.VerifyNoOtherCalls();

        _loggerMock.VerifyLogger(LogLevel.Information, "Existing participant management record found, updating record 123");
        _loggerMock.VerifyLogger(LogLevel.Information, "Participant 123 still maintained as High Risk");
    }

    [TestMethod]
    public async Task Run_WhenNonVhrParticipantExistsWithNullVhrFlag_LeavesVhrFlagAsNull()
    {
        // Arrange
        var json = JsonSerializer.Serialize(new ParticipantDemographic
        {
            NhsNumber = _serviceNowParticipant.NhsNumber,
            GivenName = _serviceNowParticipant.FirstName,
            FamilyName = _serviceNowParticipant.FamilyName,
            DateOfBirth = _serviceNowParticipant.DateOfBirth.ToString("yyyy-MM-dd")
        });

        var existingParticipant = new ParticipantManagement
        {
            ParticipantId = 123,
            ScreeningId = _serviceNowParticipant.ScreeningId,
            NHSNumber = _serviceNowParticipant.NhsNumber,
            IsHigherRisk = null
        };

        _httpClientFunctionMock.Setup(x => x.SendGetResponse($"{_configMock.Object.Value.RetrievePdsDemographicURL}?nhsNumber={_serviceNowParticipant.NhsNumber}"))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            }).Verifiable();

        _dataServiceClientMock.Setup(client => client.GetSingleByFilter(
            It.Is<Expression<Func<ParticipantManagement, bool>>>(x =>
                x.Compile().Invoke(new ParticipantManagement { NHSNumber = _serviceNowParticipant.NhsNumber, ScreeningId = _serviceNowParticipant.ScreeningId })
            ))).ReturnsAsync(existingParticipant).Verifiable();

        _dataServiceClientMock.Setup(x => x.Update(It.Is<ParticipantManagement>(p =>
                p.ParticipantId == 123 &&
                p.ScreeningId == _serviceNowParticipant.ScreeningId &&
                p.NHSNumber == _serviceNowParticipant.NhsNumber &&
                p.RecordType == Actions.Amended &&
                p.EligibilityFlag == 1 &&
                p.ReferralFlag == 1 &&
                p.IsHigherRisk == null &&
                p.RecordUpdateDateTime != null)))
            .ReturnsAsync(true).Verifiable();

        // Act
        await _function.Run(_serviceNowParticipant);

        // Assert
        _httpClientFunctionMock.Verify();
        _httpClientFunctionMock.VerifyNoOtherCalls();

        _handleExceptionMock.VerifyNoOtherCalls();

        _dataServiceClientMock.Verify();
        _dataServiceClientMock.VerifyNoOtherCalls();

        _loggerMock.VerifyLogger(LogLevel.Information, "Existing participant management record found, updating record 123");
    }
}
