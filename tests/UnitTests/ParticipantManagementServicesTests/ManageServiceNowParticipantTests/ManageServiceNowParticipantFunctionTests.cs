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
    private readonly Mock<IQueueClient> _queueClientMock = new();
    private readonly ServiceNowParticipant _serviceNowParticipant;
    private readonly ManageServiceNowParticipantFunction _function;
    private readonly string _messageType1Request;
    private readonly string _messageType2Request;
    private readonly PdsDemographic _matchingPdsDemographic;

    public ManageServiceNowParticipantFunctionTests()
    {
        _serviceNowParticipant = new ServiceNowParticipant()
        {
            ScreeningId = 1,
            NhsNumber = 1234567890,
            FirstName = "Samantha",
            FamilyName = "Bloggs",
            DateOfBirth = new DateOnly(1970, 1, 1),
            ServiceNowCaseNumber = "CS123",
            BsoCode = "ABC",
            ReasonForAdding = ServiceNowReasonsForAdding.RequiresCeasing,
            RequiredGpCode = "ZZZ123"
        };

        var config = new ManageServiceNowParticipantConfig
        {
            RetrievePdsDemographicURL = "http://localhost:8082/api/RetrievePDSDemographic",
            SendServiceNowMessageURL = "http://localhost:9092/api/servicenow/send",
            ParticipantManagementURL = "http://localhost:7994/api/ParticipantManagementDataService",
            ServiceBusConnectionString_client_internal = "Endpoint=",
            CohortDistributionTopic = "cohort-distribution-topic",
            ManageNemsSubscriptionSubscribeURL = "http://localhost:9081/api/ManageNemsSubscriptionSubscribeURL"
        };
        _configMock.Setup(c => c.Value).Returns(config);

        _function = new ManageServiceNowParticipantFunction(
            _loggerMock.Object,
            _configMock.Object,
            _httpClientFunctionMock.Object,
            _handleExceptionMock.Object,
            _dataServiceClientMock.Object,
            _queueClientMock.Object
        );

        _messageType1Request = JsonSerializer.Serialize(new SendServiceNowMessageRequestBody
        {
            MessageType = ServiceNowMessageType.UnableToAddParticipant
        });
        _messageType2Request = JsonSerializer.Serialize(new SendServiceNowMessageRequestBody
        {
            MessageType = ServiceNowMessageType.AddRequestInProgress
        });

        _matchingPdsDemographic = new PdsDemographic
        {
            NhsNumber = _serviceNowParticipant.NhsNumber.ToString(),
            FirstName = _serviceNowParticipant.FirstName,
            FamilyName = _serviceNowParticipant.FamilyName,
            DateOfBirth = _serviceNowParticipant.DateOfBirth.ToString("yyyy-MM-dd"),
            ReasonForRemoval = "ABC",
            RemovalEffectiveFromDate = "2020-01-01T00:00:00+00:00"
        };
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
        _httpClientFunctionMock.Verify(x => x.SendPut($"{_configMock.Object.Value.SendServiceNowMessageURL}/{_serviceNowParticipant.ServiceNowCaseNumber}", _messageType1Request), Times.Once());
        _httpClientFunctionMock.Verify();
        _httpClientFunctionMock.VerifyNoOtherCalls();

        _handleExceptionMock.Verify(x => x.CreateSystemExceptionLog(
                It.IsAny<Exception>(),
                It.Is<ServiceNowParticipant>(p => p.NhsNumber == _serviceNowParticipant.NhsNumber
                && p.FirstName == _serviceNowParticipant.FirstName
                && p.FamilyName == _serviceNowParticipant.FamilyName
                && p.DateOfBirth == _serviceNowParticipant.DateOfBirth
                && p.ServiceNowCaseNumber == _serviceNowParticipant.ServiceNowCaseNumber)), Times.Once);
        _handleExceptionMock.VerifyNoOtherCalls();

        _dataServiceClientMock.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task Run_WhenNhsNumberSuperseded_SendsServiceNowMessageType1()
    {
        // Arrange
        var json = JsonSerializer.Serialize(new PdsDemographic
        {
            NhsNumber = "123"
        });
        _httpClientFunctionMock.Setup(x => x.SendGetResponse($"{_configMock.Object.Value.RetrievePdsDemographicURL}?nhsNumber={_serviceNowParticipant.NhsNumber}"))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            }).Verifiable();

        // Act
        await _function.Run(_serviceNowParticipant);

        // Assert
        _httpClientFunctionMock.Verify(x => x.SendPut($"{_configMock.Object.Value.SendServiceNowMessageURL}/{_serviceNowParticipant.ServiceNowCaseNumber}", _messageType1Request), Times.Once());
        _httpClientFunctionMock.Verify();
        _httpClientFunctionMock.VerifyNoOtherCalls();

        _handleExceptionMock.Verify(x => x.CreateSystemExceptionLog(
            It.Is<Exception>(e => e.Message == "NHS Numbers don't match for ServiceNow Participant and PDS, NHS Number must have been superseded"),
            It.Is<ServiceNowParticipant>(p => p.NhsNumber == _serviceNowParticipant.NhsNumber
                && p.FirstName == _serviceNowParticipant.FirstName
                && p.FamilyName == _serviceNowParticipant.FamilyName
                && p.DateOfBirth == _serviceNowParticipant.DateOfBirth
                && p.ServiceNowCaseNumber == _serviceNowParticipant.ServiceNowCaseNumber)), Times.Once);
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
        _httpClientFunctionMock.Verify(x => x.SendPut($"{_configMock.Object.Value.SendServiceNowMessageURL}/{_serviceNowParticipant.ServiceNowCaseNumber}", _messageType2Request), Times.Once());
        _httpClientFunctionMock.Verify();
        _httpClientFunctionMock.VerifyNoOtherCalls();

        _handleExceptionMock.Verify(x => x.CreateSystemExceptionLog(
            It.Is<Exception>(e => e.Message == $"Request to PDS for ServiceNow Participant returned an unexpected response. Status code: {httpStatusCode}"),
            It.Is<ServiceNowParticipant>(p => p.NhsNumber == _serviceNowParticipant.NhsNumber
                && p.FirstName == _serviceNowParticipant.FirstName
                && p.FamilyName == _serviceNowParticipant.FamilyName
                && p.DateOfBirth == _serviceNowParticipant.DateOfBirth
                && p.ServiceNowCaseNumber == _serviceNowParticipant.ServiceNowCaseNumber)), Times.Once);
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
        _httpClientFunctionMock.Verify(x => x.SendPut($"{_configMock.Object.Value.SendServiceNowMessageURL}/{_serviceNowParticipant.ServiceNowCaseNumber}", _messageType2Request), Times.Once());
        _httpClientFunctionMock.Verify();
        _httpClientFunctionMock.VerifyNoOtherCalls();

        _handleExceptionMock.Verify(x => x.CreateSystemExceptionLog(
            expectedException,
            It.Is<ServiceNowParticipant>(p => p.NhsNumber == _serviceNowParticipant.NhsNumber
                && p.FirstName == _serviceNowParticipant.FirstName
                && p.FamilyName == _serviceNowParticipant.FamilyName
                && p.DateOfBirth == _serviceNowParticipant.DateOfBirth
                && p.ServiceNowCaseNumber == _serviceNowParticipant.ServiceNowCaseNumber)));
        _handleExceptionMock.VerifyNoOtherCalls();

        _dataServiceClientMock.VerifyNoOtherCalls();
    }

    [TestMethod]
    [DataRow("Sam", "Bloggs", "1970-01-01")]        // First names don't match
    [DataRow("Samantha", "Smith", "1970-01-01")]    // Family names don't match
    [DataRow("Samantha", "Bloggs", "1970-01-02")]   // Dates of birth don't match
    public async Task Run_WhenParticipantDataDoesNotMatchPdsData_SendsServiceNowMessageType1(
        string firstName, string familyName, string dateOfBirth)
    {
        // Arrange
        var json = JsonSerializer.Serialize(new PdsDemographic
        {
            NhsNumber = _serviceNowParticipant.NhsNumber.ToString(),
            FirstName = firstName,
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
        _httpClientFunctionMock.Verify(x => x.SendPut($"{_configMock.Object.Value.SendServiceNowMessageURL}/{_serviceNowParticipant.ServiceNowCaseNumber}", _messageType1Request), Times.Once());
        _httpClientFunctionMock.Verify();
        _httpClientFunctionMock.VerifyNoOtherCalls();

        _handleExceptionMock.Verify(x => x.CreateSystemExceptionLog(
            It.Is<Exception>(e => e.Message == "Participant data from ServiceNow does not match participant data from PDS"),
            It.Is<ServiceNowParticipant>(p => p.NhsNumber == _serviceNowParticipant.NhsNumber
                && p.FirstName == _serviceNowParticipant.FirstName
                && p.FamilyName == _serviceNowParticipant.FamilyName
                && p.DateOfBirth == _serviceNowParticipant.DateOfBirth
                && p.ServiceNowCaseNumber == _serviceNowParticipant.ServiceNowCaseNumber)), Times.Once);
        _handleExceptionMock.VerifyNoOtherCalls();

        _dataServiceClientMock.VerifyNoOtherCalls();
    }

    [TestMethod]
    [DataRow("Samantha", "Bloggs", "1970-01-01")]   // Valid - exact match
    [DataRow("SAMANTHA", "bloggs", "1970-01-01")]   // Valid - only differs by casing
    public async Task Run_WhenServiceNowParticipantIsValidAndDoesNotExistInTheDataStore_AddsTheNewParticipant(
        string firstName, string familyName, string dateOfBirth)
    {
        // Arrange
        _matchingPdsDemographic.FirstName = firstName;
        _matchingPdsDemographic.FamilyName = familyName;
        _matchingPdsDemographic.DateOfBirth = dateOfBirth;
        var json = JsonSerializer.Serialize(_matchingPdsDemographic);
        _httpClientFunctionMock.Setup(x => x.SendGetResponse($"{_configMock.Object.Value.RetrievePdsDemographicURL}?nhsNumber={_serviceNowParticipant.NhsNumber}"))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            }).Verifiable();
        _httpClientFunctionMock.Setup(x => x.SendPost(_configMock.Object.Value.ManageNemsSubscriptionSubscribeURL,
                It.Is<Dictionary<string, string>>(
                    x => x.Count == 1 && x.First().Key == "nhsNumber" && x.First().Value == _serviceNowParticipant.NhsNumber.ToString())))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)).Verifiable();

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
                p.RecordInsertDateTime != null &&
                p.ReasonForRemoval == "ABC" &&
                p.ReasonForRemovalDate == new DateTime(2020, 1, 1))))
            .ReturnsAsync(true).Verifiable();

        _queueClientMock.Setup(x => x.AddAsync(It.Is<BasicParticipantCsvRecord>(x =>
                    x.FileName == _serviceNowParticipant.ServiceNowCaseNumber &&
                    x.BasicParticipantData.ScreeningId == _serviceNowParticipant.ScreeningId.ToString() &&
                    x.BasicParticipantData.NhsNumber == _serviceNowParticipant.NhsNumber.ToString() &&
                    x.BasicParticipantData.RecordType == Actions.New &&
                    x.Participant.ReferralFlag == "1" &&
                    x.Participant.PrimaryCareProvider == _serviceNowParticipant.RequiredGpCode &&
                    x.Participant.ScreeningAcronym == "BSS"),
                _configMock.Object.Value.CohortDistributionTopic))
            .ReturnsAsync(true).Verifiable();

        // Act
        await _function.Run(_serviceNowParticipant);

        // Assert
        _httpClientFunctionMock.Verify();
        _httpClientFunctionMock.VerifyNoOtherCalls();

        _handleExceptionMock.VerifyNoOtherCalls();

        _dataServiceClientMock.Verify();
        _dataServiceClientMock.VerifyNoOtherCalls();

        _queueClientMock.Verify();
        _queueClientMock.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task Run_WhenServiceNowParticipantIsValidAndExistsInTheDataStoreButIsBlocked_DoesNotUpdateTheParticipantAndSendsMessageType1()
    {
        // Arrange
        var json = JsonSerializer.Serialize(_matchingPdsDemographic);
        _httpClientFunctionMock.Setup(x => x.SendGetResponse($"{_configMock.Object.Value.RetrievePdsDemographicURL}?nhsNumber={_serviceNowParticipant.NhsNumber}"))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            }).Verifiable();

        _dataServiceClientMock.Setup(client => client.GetSingleByFilter(
            It.Is<Expression<Func<ParticipantManagement, bool>>>(x =>
                x.Compile().Invoke(new ParticipantManagement { NHSNumber = _serviceNowParticipant.NhsNumber, ScreeningId = _serviceNowParticipant.ScreeningId })
            ))).ReturnsAsync(new ParticipantManagement { ParticipantId = 123, BlockedFlag = 1 }).Verifiable();

        _queueClientMock.Setup(x => x.AddAsync(It.Is<BasicParticipantCsvRecord>(x =>
                    x.FileName == _serviceNowParticipant.ServiceNowCaseNumber &&
                    x.BasicParticipantData.ScreeningId == _serviceNowParticipant.ScreeningId.ToString() &&
                    x.BasicParticipantData.NhsNumber == _serviceNowParticipant.NhsNumber.ToString() &&
                    x.BasicParticipantData.RecordType == Actions.Amended &&
                    x.Participant.ReferralFlag == "1" &&
                    x.Participant.PrimaryCareProvider == _serviceNowParticipant.RequiredGpCode &&
                    x.Participant.ScreeningAcronym == "BSS"),
                _configMock.Object.Value.CohortDistributionTopic))
            .ReturnsAsync(true).Verifiable();

        // Act
        await _function.Run(_serviceNowParticipant);

        // Assert
        _httpClientFunctionMock.Verify(x => x.SendPut($"{_configMock.Object.Value.SendServiceNowMessageURL}/{_serviceNowParticipant.ServiceNowCaseNumber}", _messageType1Request), Times.Once());
        _httpClientFunctionMock.Verify();
        _httpClientFunctionMock.VerifyNoOtherCalls();

        _handleExceptionMock.Verify(x => x.CreateSystemExceptionLog(
            It.Is<Exception>(e => e.Message == "Participant data from ServiceNow is blocked"),
            It.Is<ServiceNowParticipant>(p => p.NhsNumber == _serviceNowParticipant.NhsNumber
                && p.FirstName == _serviceNowParticipant.FirstName
                && p.FamilyName == _serviceNowParticipant.FamilyName
                && p.DateOfBirth == _serviceNowParticipant.DateOfBirth
                && p.ServiceNowCaseNumber == _serviceNowParticipant.ServiceNowCaseNumber)), Times.Once);
        _handleExceptionMock.VerifyNoOtherCalls();

        _dataServiceClientMock.Verify();
        _dataServiceClientMock.VerifyNoOtherCalls();

        _queueClientMock.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task Run_WhenServiceNowParticipantIsValidAndExistsInTheDataStoreAndIsNotBlocked_UpdatesTheParticipant()
    {
        // Arrange
        var json = JsonSerializer.Serialize(_matchingPdsDemographic);
        _httpClientFunctionMock.Setup(x => x.SendGetResponse($"{_configMock.Object.Value.RetrievePdsDemographicURL}?nhsNumber={_serviceNowParticipant.NhsNumber}"))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            }).Verifiable();
        _httpClientFunctionMock.Setup(x => x.SendPost(_configMock.Object.Value.ManageNemsSubscriptionSubscribeURL,
                It.Is<Dictionary<string, string>>(
                    x => x.Count == 1 && x.First().Key == "nhsNumber" && x.First().Value == _serviceNowParticipant.NhsNumber.ToString())))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)).Verifiable();

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
                p.RecordUpdateDateTime != null &&
                p.ReasonForRemoval == "ABC" &&
                p.ReasonForRemovalDate == new DateTime(2020, 1, 1))))
            .ReturnsAsync(true).Verifiable();

        _queueClientMock.Setup(x => x.AddAsync(It.Is<BasicParticipantCsvRecord>(x =>
                    x.FileName == _serviceNowParticipant.ServiceNowCaseNumber &&
                    x.BasicParticipantData.ScreeningId == _serviceNowParticipant.ScreeningId.ToString() &&
                    x.BasicParticipantData.NhsNumber == _serviceNowParticipant.NhsNumber.ToString() &&
                    x.BasicParticipantData.RecordType == Actions.Amended &&
                    x.Participant.ReferralFlag == "1" &&
                    x.Participant.PrimaryCareProvider == _serviceNowParticipant.RequiredGpCode &&
                    x.Participant.ScreeningAcronym == "BSS"),
                _configMock.Object.Value.CohortDistributionTopic))
            .ReturnsAsync(true).Verifiable();

        // Act
        await _function.Run(_serviceNowParticipant);

        // Assert
        _httpClientFunctionMock.Verify();
        _httpClientFunctionMock.VerifyNoOtherCalls();

        _handleExceptionMock.VerifyNoOtherCalls();

        _dataServiceClientMock.Verify();
        _dataServiceClientMock.VerifyNoOtherCalls();

        _queueClientMock.Verify();
        _queueClientMock.VerifyNoOtherCalls();
    }

    [TestMethod]
    [DataRow(HttpStatusCode.BadRequest)]
    [DataRow(HttpStatusCode.InternalServerError)]
    public async Task Run_WhenSubscribesToNEMSFails_LogsErrorMessageButContinues(HttpStatusCode httpStatusCode)
    {
        // Arrange
        var json = JsonSerializer.Serialize(_matchingPdsDemographic);
        _httpClientFunctionMock.Setup(x => x.SendGetResponse($"{_configMock.Object.Value.RetrievePdsDemographicURL}?nhsNumber={_serviceNowParticipant.NhsNumber}"))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            }).Verifiable();
        _httpClientFunctionMock.Setup(x => x.SendPost(_configMock.Object.Value.ManageNemsSubscriptionSubscribeURL,
                It.Is<Dictionary<string, string>>(
                    x => x.Count == 1 && x.First().Key == "nhsNumber" && x.First().Value == _serviceNowParticipant.NhsNumber.ToString())))
            .ReturnsAsync(new HttpResponseMessage(httpStatusCode)).Verifiable();

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

        _queueClientMock.Setup(x => x.AddAsync(It.Is<BasicParticipantCsvRecord>(x =>
                    x.FileName == _serviceNowParticipant.ServiceNowCaseNumber &&
                    x.BasicParticipantData.ScreeningId == _serviceNowParticipant.ScreeningId.ToString() &&
                    x.BasicParticipantData.NhsNumber == _serviceNowParticipant.NhsNumber.ToString() &&
                    x.BasicParticipantData.RecordType == Actions.Amended &&
                    x.Participant.ReferralFlag == "1" &&
                    x.Participant.PrimaryCareProvider == _serviceNowParticipant.RequiredGpCode &&
                    x.Participant.ScreeningAcronym == "BSS"),
                _configMock.Object.Value.CohortDistributionTopic))
            .ReturnsAsync(true).Verifiable();

        // Act
        await _function.Run(_serviceNowParticipant);

        // Assert
        _httpClientFunctionMock.Verify();
        _httpClientFunctionMock.VerifyNoOtherCalls();

        _handleExceptionMock.VerifyNoOtherCalls();

        _dataServiceClientMock.Verify();
        _dataServiceClientMock.VerifyNoOtherCalls();

        _queueClientMock.Verify();
        _queueClientMock.VerifyNoOtherCalls();

        _loggerMock.VerifyLogger(LogLevel.Error, $"Failed to subscribe participant for updates. Case Number: {_serviceNowParticipant.ServiceNowCaseNumber}");
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
            ServiceNowCaseNumber = "CS123",
            BsoCode = "ABC",
            ReasonForAdding = ServiceNowReasonsForAdding.VeryHighRisk,
            RequiredGpCode = "T35 7ING"
        };

        var json = JsonSerializer.Serialize(_matchingPdsDemographic);

        _httpClientFunctionMock.Setup(x => x.SendGetResponse($"{_configMock.Object.Value.RetrievePdsDemographicURL}?nhsNumber={vhrParticipant.NhsNumber}"))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            }).Verifiable();
        _httpClientFunctionMock.Setup(x => x.SendPost(_configMock.Object.Value.ManageNemsSubscriptionSubscribeURL,
                It.Is<Dictionary<string, string>>(
                    x => x.Count == 1 && x.First().Key == "nhsNumber" && x.First().Value == _serviceNowParticipant.NhsNumber.ToString())))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)).Verifiable();

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

        _queueClientMock.Setup(x => x.AddAsync(It.Is<BasicParticipantCsvRecord>(x =>
                    x.FileName == vhrParticipant.ServiceNowCaseNumber &&
                    x.BasicParticipantData.ScreeningId == vhrParticipant.ScreeningId.ToString() &&
                    x.BasicParticipantData.NhsNumber == vhrParticipant.NhsNumber.ToString() &&
                    x.BasicParticipantData.RecordType == Actions.New &&
                    x.Participant.ReferralFlag == "1" &&
                    x.Participant.PrimaryCareProvider == vhrParticipant.RequiredGpCode &&
                    x.Participant.ScreeningAcronym == "BSS"),
                _configMock.Object.Value.CohortDistributionTopic))
            .ReturnsAsync(true).Verifiable();

        // Act
        await _function.Run(vhrParticipant);

        // Assert
        _httpClientFunctionMock.Verify();
        _httpClientFunctionMock.VerifyNoOtherCalls();

        _handleExceptionMock.VerifyNoOtherCalls();

        _dataServiceClientMock.Verify();
        _dataServiceClientMock.VerifyNoOtherCalls();

        _queueClientMock.Verify();
        _queueClientMock.VerifyNoOtherCalls();

        _loggerMock.VerifyLogger(LogLevel.Information, "Participant not in participant management table, adding new record");
        _loggerMock.VerifyLogger(LogLevel.Information, $"Participant set as High Risk");
    }

    [TestMethod]
    public async Task Run_WhenServiceNowParticipantIsNotVhrAndDoesNotExistInDataStore_AddsNewParticipantWithoutVhrFlag()
    {
        // Arrange
        var json = JsonSerializer.Serialize(_matchingPdsDemographic);

        _httpClientFunctionMock.Setup(x => x.SendGetResponse($"{_configMock.Object.Value.RetrievePdsDemographicURL}?nhsNumber={_serviceNowParticipant.NhsNumber}"))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            }).Verifiable();
        _httpClientFunctionMock.Setup(x => x.SendPost(_configMock.Object.Value.ManageNemsSubscriptionSubscribeURL,
                It.Is<Dictionary<string, string>>(
                    x => x.Count == 1 && x.First().Key == "nhsNumber" && x.First().Value == _serviceNowParticipant.NhsNumber.ToString())))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)).Verifiable();

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

        _queueClientMock.Setup(x => x.AddAsync(It.Is<BasicParticipantCsvRecord>(x =>
                    x.FileName == _serviceNowParticipant.ServiceNowCaseNumber &&
                    x.BasicParticipantData.ScreeningId == _serviceNowParticipant.ScreeningId.ToString() &&
                    x.BasicParticipantData.NhsNumber == _serviceNowParticipant.NhsNumber.ToString() &&
                    x.BasicParticipantData.RecordType == Actions.New &&
                    x.Participant.ReferralFlag == "1" &&
                    x.Participant.PrimaryCareProvider == _serviceNowParticipant.RequiredGpCode &&
                    x.Participant.ScreeningAcronym == "BSS"),
                _configMock.Object.Value.CohortDistributionTopic))
            .ReturnsAsync(true).Verifiable();

        // Act
        await _function.Run(_serviceNowParticipant);

        // Assert
        _httpClientFunctionMock.Verify();
        _httpClientFunctionMock.VerifyNoOtherCalls();

        _handleExceptionMock.VerifyNoOtherCalls();

        _dataServiceClientMock.Verify();
        _dataServiceClientMock.VerifyNoOtherCalls();

        _queueClientMock.Verify();
        _queueClientMock.VerifyNoOtherCalls();

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
            ServiceNowCaseNumber = "CS123",
            BsoCode = "ABC",
            ReasonForAdding = ServiceNowReasonsForAdding.VeryHighRisk,
            RequiredGpCode = "T35 7ING"
        };

        var json = JsonSerializer.Serialize(_matchingPdsDemographic);

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
        _httpClientFunctionMock.Setup(x => x.SendPost(_configMock.Object.Value.ManageNemsSubscriptionSubscribeURL,
                It.Is<Dictionary<string, string>>(
                    x => x.Count == 1 && x.First().Key == "nhsNumber" && x.First().Value == _serviceNowParticipant.NhsNumber.ToString())))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)).Verifiable();

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

        _queueClientMock.Setup(x => x.AddAsync(It.Is<BasicParticipantCsvRecord>(x =>
                    x.FileName == vhrParticipant.ServiceNowCaseNumber &&
                    x.BasicParticipantData.ScreeningId == vhrParticipant.ScreeningId.ToString() &&
                    x.BasicParticipantData.NhsNumber == vhrParticipant.NhsNumber.ToString() &&
                    x.BasicParticipantData.RecordType == Actions.Amended &&
                    x.Participant.ReferralFlag == "1" &&
                    x.Participant.PrimaryCareProvider == vhrParticipant.RequiredGpCode &&
                    x.Participant.ScreeningAcronym == "BSS"),
                _configMock.Object.Value.CohortDistributionTopic))
            .ReturnsAsync(true).Verifiable();

        // Act
        await _function.Run(vhrParticipant);

        // Assert
        _httpClientFunctionMock.Verify();
        _httpClientFunctionMock.VerifyNoOtherCalls();

        _handleExceptionMock.VerifyNoOtherCalls();

        _dataServiceClientMock.Verify();
        _dataServiceClientMock.VerifyNoOtherCalls();

        _queueClientMock.Verify();
        _queueClientMock.VerifyNoOtherCalls();

        _loggerMock.VerifyLogger(LogLevel.Information, "Existing participant management record found, updating record 123");
        _loggerMock.VerifyLogger(LogLevel.Information, "Participant 123 set as High Risk based on ServiceNow attributes");
        _loggerMock.VerifyLogger(LogLevel.Information, "Participant 123 still maintained as High Risk");
    }

    [TestMethod]
    public async Task Run_WhenParticipantExistsWithVhrFlagAlreadySet_MaintainsVhrFlag()
    {
        // Arrange
        var json = JsonSerializer.Serialize(_matchingPdsDemographic);

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
        _httpClientFunctionMock.Setup(x => x.SendPost(_configMock.Object.Value.ManageNemsSubscriptionSubscribeURL,
                It.Is<Dictionary<string, string>>(
                    x => x.Count == 1 && x.First().Key == "nhsNumber" && x.First().Value == _serviceNowParticipant.NhsNumber.ToString())))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)).Verifiable();

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

        _queueClientMock.Setup(x => x.AddAsync(It.Is<BasicParticipantCsvRecord>(x =>
                    x.FileName == _serviceNowParticipant.ServiceNowCaseNumber &&
                    x.BasicParticipantData.ScreeningId == _serviceNowParticipant.ScreeningId.ToString() &&
                    x.BasicParticipantData.NhsNumber == _serviceNowParticipant.NhsNumber.ToString() &&
                    x.BasicParticipantData.RecordType == Actions.Amended &&
                    x.Participant.ReferralFlag == "1" &&
                    x.Participant.PrimaryCareProvider == _serviceNowParticipant.RequiredGpCode &&
                    x.Participant.ScreeningAcronym == "BSS"),
                _configMock.Object.Value.CohortDistributionTopic))
            .ReturnsAsync(true).Verifiable();

        // Act
        await _function.Run(_serviceNowParticipant);

        // Assert
        _httpClientFunctionMock.Verify();
        _httpClientFunctionMock.VerifyNoOtherCalls();

        _handleExceptionMock.VerifyNoOtherCalls();

        _dataServiceClientMock.Verify();
        _dataServiceClientMock.VerifyNoOtherCalls();

        _queueClientMock.Verify();
        _queueClientMock.VerifyNoOtherCalls();

        _loggerMock.VerifyLogger(LogLevel.Information, "Existing participant management record found, updating record 123");
        _loggerMock.VerifyLogger(LogLevel.Information, "Participant 123 still maintained as High Risk");
    }

    [TestMethod]
    public async Task Run_WhenNonVhrParticipantExistsWithNullVhrFlag_LeavesVhrFlagAsNull()
    {
        // Arrange
        var json = JsonSerializer.Serialize(_matchingPdsDemographic);

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
        _httpClientFunctionMock.Setup(x => x.SendPost(_configMock.Object.Value.ManageNemsSubscriptionSubscribeURL,
                It.Is<Dictionary<string, string>>(
                    x => x.Count == 1 && x.First().Key == "nhsNumber" && x.First().Value == _serviceNowParticipant.NhsNumber.ToString())))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)).Verifiable();

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

        _queueClientMock.Setup(x => x.AddAsync(It.Is<BasicParticipantCsvRecord>(x =>
                    x.FileName == _serviceNowParticipant.ServiceNowCaseNumber &&
                    x.BasicParticipantData.ScreeningId == _serviceNowParticipant.ScreeningId.ToString() &&
                    x.BasicParticipantData.NhsNumber == _serviceNowParticipant.NhsNumber.ToString() &&
                    x.BasicParticipantData.RecordType == Actions.Amended &&
                    x.Participant.ReferralFlag == "1" &&
                    x.Participant.PrimaryCareProvider == _serviceNowParticipant.RequiredGpCode &&
                    x.Participant.ScreeningAcronym == "BSS"),
                _configMock.Object.Value.CohortDistributionTopic))
            .ReturnsAsync(true).Verifiable();

        // Act
        await _function.Run(_serviceNowParticipant);

        // Assert
        _httpClientFunctionMock.Verify();
        _httpClientFunctionMock.VerifyNoOtherCalls();

        _handleExceptionMock.VerifyNoOtherCalls();

        _dataServiceClientMock.Verify();
        _dataServiceClientMock.VerifyNoOtherCalls();

        _queueClientMock.Verify();
        _queueClientMock.VerifyNoOtherCalls();

        _loggerMock.VerifyLogger(LogLevel.Information, "Existing participant management record found, updating record 123");
    }

    [TestMethod]
    [DataRow("Samantha", "Bloggs", "Samantha ", "Bloggs", "1970-01-01")]          // Trailing space in first name from PDS
    [DataRow("Samantha", "Bloggs", "Samantha", "Bloggs ", "1970-01-01")]          // Trailing space in family name from PDS
    [DataRow("Samantha", "Bloggs", " Samantha", "Bloggs", "1970-01-01")]          // Leading space in first name from PDS
    [DataRow("Samantha", "Bloggs", "Samantha", " Bloggs", "1970-01-01")]          // Leading space in family name from PDS
    [DataRow("Samantha", "Bloggs", "Samantha  ", "Bloggs  ", "1970-01-01")]       // Multiple trailing spaces from PDS
    [DataRow("MaryAnne", "Smith", "Mary-Anne", "Smith", "1970-01-01")]            // ServiceNow has no hyphen, PDS has hyphen
    [DataRow("Mary-Anne", "Smith", "MaryAnne", "Smith", "1970-01-01")]            // ServiceNow has hyphen, PDS has no hyphen
    [DataRow("Mary-Anne", "Smith", "Mary Anne", "Smith", "1970-01-01")]           // ServiceNow has hyphen, PDS has space
    [DataRow("Mary-Anne", "Smith-Jones", "MaryAnne", "SmithJones", "1970-01-01")] // Hyphens in ServiceNow, none in PDS
    [DataRow("MaryAnne", "SmithJones", "Mary Anne", "Smith Jones", "1970-01-01")] // No hyphens in ServiceNow, spaces in PDS
    [DataRow("Mary-Anne", "Smith-Jones", "Mary Anne", "Smith Jones", "1970-01-01")] // Hyphens in ServiceNow, spaces in PDS
    [DataRow("José", "Bloggs", "José ", "Bloggs", "1970-01-01")]                 // Accented character with trailing space
    [DataRow("François", "Müller", "François ", "Müller ", "1970-01-01")]        // Multiple accented characters with trailing spaces
    [DataRow("Siobhán", "OBrien", "Siobhán", "O'Brien", "1970-01-01")]           // Accented character, apostrophe removed
    [DataRow("Samantha", "OBrien", "Samantha", "O'Brien", "1970-01-01")]         // ServiceNow without apostrophe, PDS with apostrophe
    [DataRow("Samantha", "dArcy", "Samantha", "d'Arcy", "1970-01-01")]           // Lowercase name with apostrophe in PDS
    [DataRow("samantha", "bloggs", "SAMANTHA", "BLOGGS", "1970-01-01")]         // Case insensitive matching
    [DataRow(" Sámañtha ", " Blóggs ", "SÂMÁNTHÅ", "BLÓGGṠ", "1970-01-01")]  // Realistic accents with spaces
    [DataRow(" SÁmʹañ. t,h ã ", " Błó,gʼg s̈. ", "SÂMANTHÅ", "BŁÓGGṠ", "1970-01-01")] // Multiple special chars and accents
    public async Task Run_WhenServiceNowParticipantNameHasTrailingSpacesOrHyphensOrSpecialChars_MatchesWithPdsAndAddsParticipant(
        string serviceNowFirstName, string serviceNowFamilyName, string pdsFirstName, string pdsFamilyName, string dateOfBirth)
    {
        // Arrange
        var testServiceNowParticipant = new ServiceNowParticipant
        {
            ScreeningId = _serviceNowParticipant.ScreeningId,
            NhsNumber = _serviceNowParticipant.NhsNumber,
            FirstName = serviceNowFirstName,
            FamilyName = serviceNowFamilyName,
            DateOfBirth = _serviceNowParticipant.DateOfBirth,
            ServiceNowCaseNumber = _serviceNowParticipant.ServiceNowCaseNumber,
            BsoCode = _serviceNowParticipant.BsoCode,
            ReasonForAdding = _serviceNowParticipant.ReasonForAdding,
            RequiredGpCode = _serviceNowParticipant.RequiredGpCode
        };

        var testPdsDemographic = new PdsDemographic
        {
            NhsNumber = _matchingPdsDemographic.NhsNumber,
            FirstName = pdsFirstName,
            FamilyName = pdsFamilyName,
            DateOfBirth = dateOfBirth,
            ReasonForRemoval = _matchingPdsDemographic.ReasonForRemoval,
            RemovalEffectiveFromDate = _matchingPdsDemographic.RemovalEffectiveFromDate
        };

        var json = JsonSerializer.Serialize(testPdsDemographic);
        _httpClientFunctionMock.Setup(x => x.SendGetResponse($"{_configMock.Object.Value.RetrievePdsDemographicURL}?nhsNumber={testServiceNowParticipant.NhsNumber}"))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            }).Verifiable();

        _httpClientFunctionMock.Setup(x => x.SendPost(_configMock.Object.Value.ManageNemsSubscriptionSubscribeURL,
                It.Is<Dictionary<string, string>>(x => x.Count == 1 && x.First().Key == "nhsNumber" && x.First().Value == testServiceNowParticipant.NhsNumber.ToString())))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)).Verifiable();

        _dataServiceClientMock.Setup(client => client.GetSingleByFilter(
            It.Is<Expression<Func<ParticipantManagement, bool>>>(x => x.Compile().Invoke(new ParticipantManagement { NHSNumber = testServiceNowParticipant.NhsNumber, ScreeningId = testServiceNowParticipant.ScreeningId })
            ))).ReturnsAsync((ParticipantManagement)null!).Verifiable();

        _dataServiceClientMock.Setup(x => x.Add(It.IsAny<ParticipantManagement>())).ReturnsAsync(true).Verifiable();
        _queueClientMock.Setup(x => x.AddAsync(It.IsAny<BasicParticipantCsvRecord>(), _configMock.Object.Value.CohortDistributionTopic)).ReturnsAsync(true).Verifiable();

        // Act
        await _function.Run(testServiceNowParticipant);

        // Assert
        _httpClientFunctionMock.Verify();
        _dataServiceClientMock.Verify(x => x.Add(It.IsAny<ParticipantManagement>()), Times.Once);
        _queueClientMock.Verify();

        _httpClientFunctionMock.Verify(
            x => x.SendPut(It.IsAny<string>(), _messageType1Request),
            Times.Never,
            "Should not send UnableToAddParticipant message when names match after normalization");
    }

    [TestMethod]
    [DataRow("Victoria", "Smith", "1970-01-01")]            // Completely different first name
    [DataRow("Samantha", "Williams", "1970-01-01")]         // Completely different family name
    [DataRow("Sam", "Bloggs", "1970-01-01")]                // Shortened first name (not just formatting)
    [DataRow("Samantha-Jane", "Bloggs", "1970-01-01")]      // Additional name part
    [DataRow("François", "Bloggs", "1970-01-01")]           // ServiceNow without accent, PDS with accent - should NOT match
    [DataRow("Siobhán", "Bloggs", "1970-01-01")]            // ServiceNow without accent, PDS with accent - should NOT match
    public async Task Run_WhenServiceNowParticipantNamesDontMatchPdsAfterNormalization_ParticipantDataDoesNotMatchExceptionRaised(string pdsFirstName, string pdsFamilyName, string dateOfBirth)
    {
        // Arrange
        _matchingPdsDemographic.FirstName = pdsFirstName;
        _matchingPdsDemographic.FamilyName = pdsFamilyName;
        _matchingPdsDemographic.DateOfBirth = dateOfBirth;

        var json = JsonSerializer.Serialize(_matchingPdsDemographic);
        _httpClientFunctionMock.Setup(x => x.SendGetResponse($"{_configMock.Object.Value.RetrievePdsDemographicURL}?nhsNumber={_serviceNowParticipant.NhsNumber}"))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            }).Verifiable();

        // Act
        await _function.Run(_serviceNowParticipant);

        // Assert
        _httpClientFunctionMock.Verify(x => x.SendPut($"{_configMock.Object.Value.SendServiceNowMessageURL}/{_serviceNowParticipant.ServiceNowCaseNumber}", _messageType1Request), Times.Once());
        _handleExceptionMock.Verify(x => x.CreateSystemExceptionLog(It.Is<Exception>(e => e.Message == "Participant data from ServiceNow does not match participant data from PDS"),
            It.IsAny<ServiceNowParticipant>()), Times.Once);
        _dataServiceClientMock.Verify(x => x.Add(It.IsAny<ParticipantManagement>()), Times.Never);
        _queueClientMock.Verify(x => x.AddAsync(It.IsAny<BasicParticipantCsvRecord>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    [DataRow("123", "456", "1970-01-01")]      // Numbers only - both normalize to empty strings
    [DataRow("123", "123", "1970-01-01")]      // Numbers only - Matching
    [DataRow("---", "@@@", "1970-01-01")]      // Symbols only - both normalize to empty strings
    [DataRow("123-456", "...", "1970-01-01")]  // Mixed non-letters - both normalize to empty strings
    [DataRow("   ", "   ", "1970-01-01")]      // Spaces only
    public async Task Run_WhenNamesContainOnlyNonLetterCharacters_ParticipantDataDoesNotMatchExceptionRaised(string pdsFirstName, string pdsFamilyName, string dateOfBirth)
    {
        // Arrange
        var testServiceNowParticipant = new ServiceNowParticipant
        {
            ScreeningId = _serviceNowParticipant.ScreeningId,
            NhsNumber = _serviceNowParticipant.NhsNumber,
            FirstName = "123",
            FamilyName = "456",
            DateOfBirth = _serviceNowParticipant.DateOfBirth,
            ServiceNowCaseNumber = _serviceNowParticipant.ServiceNowCaseNumber,
            BsoCode = _serviceNowParticipant.BsoCode,
            ReasonForAdding = _serviceNowParticipant.ReasonForAdding,
            RequiredGpCode = _serviceNowParticipant.RequiredGpCode
        };

        var testPdsDemographic = new PdsDemographic
        {
            NhsNumber = _matchingPdsDemographic.NhsNumber,
            FirstName = pdsFirstName,
            FamilyName = pdsFamilyName,
            DateOfBirth = dateOfBirth,
            ReasonForRemoval = _matchingPdsDemographic.ReasonForRemoval,
            RemovalEffectiveFromDate = _matchingPdsDemographic.RemovalEffectiveFromDate
        };

        var json = JsonSerializer.Serialize(testPdsDemographic);
        _httpClientFunctionMock.Setup(x => x.SendGetResponse($"{_configMock.Object.Value.RetrievePdsDemographicURL}?nhsNumber={testServiceNowParticipant.NhsNumber}"))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            }).Verifiable();

        // Act
        await _function.Run(testServiceNowParticipant);

        // Assert
        _httpClientFunctionMock.Verify(x => x.SendPut($"{_configMock.Object.Value.SendServiceNowMessageURL}/{testServiceNowParticipant.ServiceNowCaseNumber}", _messageType1Request), Times.Once(),
            "Should send UnableToAddParticipant message when names normalize to empty strings");
        _handleExceptionMock.Verify(x => x.CreateSystemExceptionLog(It.Is<Exception>(e => e.Message == "Participant data from ServiceNow does not match participant data from PDS"),
            It.IsAny<ServiceNowParticipant>()), Times.Once, "Should create exception log when names containing only non-letter characters don't spuriously match");
        _dataServiceClientMock.Verify(x => x.Add(It.IsAny<ParticipantManagement>()), Times.Never, "Should not add participant when names are invalid (only non-letter characters)");
        _queueClientMock.Verify(x => x.AddAsync(It.IsAny<BasicParticipantCsvRecord>(), It.IsAny<string>()), Times.Never, "Should not queue participant when names are invalid");
    }
}
