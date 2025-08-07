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
    private readonly ParticipantDemographic _demographic;

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
            ReasonForAdding = ServiceNowReasonsForAdding.RequiresCeasing
        };

        var config = new ManageServiceNowParticipantConfig
        {
            RetrievePdsDemographicURL = "http://localhost:8082/api/RetrievePDSDemographic",
            SendServiceNowMessageURL = "http://localhost:9092/api/servicenow/send",
            ParticipantManagementURL = "http://localhost:7994/api/ParticipantManagementDataService",
            ServiceBusConnectionString_client_internal = "Endpoint=",
            CohortDistributionTopic = "cohort-distribution-topic"

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

        _demographic = new ParticipantDemographic
        {
            NhsNumber = _serviceNowParticipant.NhsNumber,
            GivenName = _serviceNowParticipant.FirstName,
            FamilyName = _serviceNowParticipant.FamilyName,
            DateOfBirth = _serviceNowParticipant.DateOfBirth.ToString("yyyy-MM-dd"),
            PostCode = "SW1A 2AA"
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
                It.Is<Exception>(e => e.Message == "Request to PDS for ServiceNow Participant returned a 404 NotFound response."),
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
    public async Task Run_WhenServiceNowParticipantIsValidAndDoesNotExistInTheDataStore_AddsTheNewParticipant()
    {
        // Arrange
        var json = JsonSerializer.Serialize(_demographic);
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

        _queueClientMock.Setup(x => x.AddAsync(It.Is<BasicParticipantCsvRecord>(x =>
                    x.FileName == _serviceNowParticipant.ServiceNowCaseNumber &&
                    x.BasicParticipantData.ScreeningId == _serviceNowParticipant.ScreeningId.ToString() &&
                    x.BasicParticipantData.NhsNumber == _serviceNowParticipant.NhsNumber.ToString() &&
                    x.BasicParticipantData.RecordType == Actions.New &&
                    x.Participant.ReferralFlag == "1" &&
                    x.Participant.Postcode == _demographic.PostCode &&
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
        var json = JsonSerializer.Serialize(_demographic);
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
                    x.Participant.Postcode == _demographic.PostCode &&
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
        var json = JsonSerializer.Serialize(_demographic);
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

        _queueClientMock.Setup(x => x.AddAsync(It.Is<BasicParticipantCsvRecord>(x =>
                    x.FileName == _serviceNowParticipant.ServiceNowCaseNumber &&
                    x.BasicParticipantData.ScreeningId == _serviceNowParticipant.ScreeningId.ToString() &&
                    x.BasicParticipantData.NhsNumber == _serviceNowParticipant.NhsNumber.ToString() &&
                    x.BasicParticipantData.RecordType == Actions.Amended &&
                    x.Participant.ReferralFlag == "1" &&
                    x.Participant.Postcode == _demographic.PostCode &&
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
            ReasonForAdding = ServiceNowReasonsForAdding.VeryHighRisk
        };

        var json = JsonSerializer.Serialize(_demographic);

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

        _queueClientMock.Setup(x => x.AddAsync(It.Is<BasicParticipantCsvRecord>(x =>
                    x.FileName == _serviceNowParticipant.ServiceNowCaseNumber &&
                    x.BasicParticipantData.ScreeningId == _serviceNowParticipant.ScreeningId.ToString() &&
                    x.BasicParticipantData.NhsNumber == _serviceNowParticipant.NhsNumber.ToString() &&
                    x.BasicParticipantData.RecordType == Actions.New &&
                    x.Participant.ReferralFlag == "1" &&
                    x.Participant.Postcode == _demographic.PostCode &&
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
        _loggerMock.VerifyLogger(LogLevel.Information, $"Participant with NHS Number: {vhrParticipant.NhsNumber} set as High Risk");
    }

    [TestMethod]
    public async Task Run_WhenServiceNowParticipantIsNotVhrAndDoesNotExistInDataStore_AddsNewParticipantWithoutVhrFlag()
    {
        // Arrange
        var json = JsonSerializer.Serialize(_demographic);

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

        _queueClientMock.Setup(x => x.AddAsync(It.Is<BasicParticipantCsvRecord>(x =>
                    x.FileName == _serviceNowParticipant.ServiceNowCaseNumber &&
                    x.BasicParticipantData.ScreeningId == _serviceNowParticipant.ScreeningId.ToString() &&
                    x.BasicParticipantData.NhsNumber == _serviceNowParticipant.NhsNumber.ToString() &&
                    x.BasicParticipantData.RecordType == Actions.New &&
                    x.Participant.ReferralFlag == "1" &&
                    x.Participant.Postcode == _demographic.PostCode &&
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
            ReasonForAdding = ServiceNowReasonsForAdding.VeryHighRisk
        };

        var json = JsonSerializer.Serialize(_demographic);

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

        _queueClientMock.Setup(x => x.AddAsync(It.Is<BasicParticipantCsvRecord>(x =>
                    x.FileName == _serviceNowParticipant.ServiceNowCaseNumber &&
                    x.BasicParticipantData.ScreeningId == _serviceNowParticipant.ScreeningId.ToString() &&
                    x.BasicParticipantData.NhsNumber == _serviceNowParticipant.NhsNumber.ToString() &&
                    x.BasicParticipantData.RecordType == Actions.Amended &&
                    x.Participant.ReferralFlag == "1" &&
                    x.Participant.Postcode == _demographic.PostCode &&
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
        var json = JsonSerializer.Serialize(_demographic);

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

        _queueClientMock.Setup(x => x.AddAsync(It.Is<BasicParticipantCsvRecord>(x =>
                    x.FileName == _serviceNowParticipant.ServiceNowCaseNumber &&
                    x.BasicParticipantData.ScreeningId == _serviceNowParticipant.ScreeningId.ToString() &&
                    x.BasicParticipantData.NhsNumber == _serviceNowParticipant.NhsNumber.ToString() &&
                    x.BasicParticipantData.RecordType == Actions.Amended &&
                    x.Participant.ReferralFlag == "1" &&
                    x.Participant.Postcode == _demographic.PostCode &&
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
        var json = JsonSerializer.Serialize(_demographic);

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

        _queueClientMock.Setup(x => x.AddAsync(It.Is<BasicParticipantCsvRecord>(x =>
                    x.FileName == _serviceNowParticipant.ServiceNowCaseNumber &&
                    x.BasicParticipantData.ScreeningId == _serviceNowParticipant.ScreeningId.ToString() &&
                    x.BasicParticipantData.NhsNumber == _serviceNowParticipant.NhsNumber.ToString() &&
                    x.BasicParticipantData.RecordType == Actions.Amended &&
                    x.Participant.ReferralFlag == "1" &&
                    x.Participant.Postcode == _demographic.PostCode &&
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
    [DataRow("ZZZ123")]
    [DataRow("zzz456")]
    public async Task Run_WhenAddingNewParticipantWithDummyGpCode_UpdatesCohortDistribution(string requiredGpCode)
    {
        // Arrange
        var participantWithGpCode = new ServiceNowParticipant()
        {
            ScreeningId = 1,
            NhsNumber = 1234567890,
            FirstName = "Samantha",
            FamilyName = "Bloggs",
            DateOfBirth = new DateOnly(1970, 1, 1),
            ServiceNowCaseNumber = "CS123",
            BsoCode = "ABC",
            ReasonForAdding = ServiceNowReasonsForAdding.RequiresCeasing,
            RequiredGpCode = requiredGpCode
        };

        SetupPdsResponse(participantWithGpCode, "REAL123");

        _dataServiceClientMock.Setup(client => client.GetSingleByFilter(
            It.Is<Expression<Func<ParticipantManagement, bool>>>(x =>
                x.Compile().Invoke(new ParticipantManagement { NHSNumber = participantWithGpCode.NhsNumber, ScreeningId = participantWithGpCode.ScreeningId })
            ))).ReturnsAsync((ParticipantManagement)null!);

        _dataServiceClientMock.Setup(x => x.Add(It.IsAny<ParticipantManagement>())).ReturnsAsync(true);

        var existingCohortDistribution = new CohortDistribution
        {
            NHSNumber = participantWithGpCode.NhsNumber,
            PrimaryCareProvider = "OLD123"
        };

        _cohortDistributionClientMock.Setup(x => x.GetSingleByFilter(
            It.Is<Expression<Func<CohortDistribution, bool>>>(expr =>
                expr.Compile().Invoke(new CohortDistribution { NHSNumber = participantWithGpCode.NhsNumber })
            ))).ReturnsAsync(existingCohortDistribution);

        _cohortDistributionClientMock.Setup(x => x.Update(It.Is<CohortDistribution>(c =>
            c.NHSNumber == participantWithGpCode.NhsNumber &&
            c.PrimaryCareProvider == requiredGpCode &&
            c.RecordUpdateDateTime != null))).ReturnsAsync(true);

        // Act
        await _function.Run(participantWithGpCode);

        // Assert
        _cohortDistributionClientMock.Verify(x => x.GetSingleByFilter(It.IsAny<Expression<Func<CohortDistribution, bool>>>()), Times.Once);
        _cohortDistributionClientMock.Verify(x => x.Update(It.IsAny<CohortDistribution>()), Times.Once);
        _loggerMock.VerifyLogger(LogLevel.Information, $"ADD participant with NHS Number: {participantWithGpCode.NhsNumber} has dummy GP code: {requiredGpCode}, updating Cohort Distribution table");
        _loggerMock.VerifyLogger(LogLevel.Information, $"Successfully updated Primary Care Provider in Cohort Distribution for NHS Number: {participantWithGpCode.NhsNumber}");
    }

    [TestMethod]
    [DataRow("ABC123")]
    [DataRow("")]
    [DataRow(null)]
    public async Task Run_WhenAddingNewParticipantWithoutDummyGpCode_DoesNotUpdateCohortDistribution(string requiredGpCode)
    {
        // Arrange
        var participantWithGpCode = new ServiceNowParticipant()
        {
            ScreeningId = 1,
            NhsNumber = 1234567890,
            FirstName = "Samantha",
            FamilyName = "Bloggs",
            DateOfBirth = new DateOnly(1970, 1, 1),
            ServiceNowCaseNumber = "CS123",
            BsoCode = "ABC",
            ReasonForAdding = ServiceNowReasonsForAdding.RequiresCeasing,
            RequiredGpCode = requiredGpCode
        };

        SetupPdsResponse(participantWithGpCode, "REAL123");

        _dataServiceClientMock.Setup(client => client.GetSingleByFilter(
            It.Is<Expression<Func<ParticipantManagement, bool>>>(x =>
                x.Compile().Invoke(new ParticipantManagement { NHSNumber = participantWithGpCode.NhsNumber, ScreeningId = participantWithGpCode.ScreeningId })
            ))).ReturnsAsync((ParticipantManagement)null!);

        _dataServiceClientMock.Setup(x => x.Add(It.IsAny<ParticipantManagement>())).ReturnsAsync(true);

        // Act
        await _function.Run(participantWithGpCode);

        // Assert
        _cohortDistributionClientMock.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task Run_WhenAmendingExistingParticipant_DoesNotUpdateWhenGpCodeIsSame()
    {
        // Arrange
        SetupPdsResponse(_serviceNowParticipant, "REAL123");

        var existingParticipant = new ParticipantManagement
        {
            ParticipantId = 123,
            ScreeningId = _serviceNowParticipant.ScreeningId,
            NHSNumber = _serviceNowParticipant.NhsNumber
        };

        _dataServiceClientMock.Setup(client => client.GetSingleByFilter(
            It.Is<Expression<Func<ParticipantManagement, bool>>>(x =>
                x.Compile().Invoke(new ParticipantManagement { NHSNumber = _serviceNowParticipant.NhsNumber, ScreeningId = _serviceNowParticipant.ScreeningId })
            ))).ReturnsAsync(existingParticipant);

        _dataServiceClientMock.Setup(x => x.Update(It.IsAny<ParticipantManagement>())).ReturnsAsync(true);

        var existingCohortDistribution = new CohortDistribution
        {
            NHSNumber = _serviceNowParticipant.NhsNumber,
            PrimaryCareProvider = "REAL123"
        };

        _cohortDistributionClientMock.Setup(x => x.GetSingleByFilter(
            It.Is<Expression<Func<CohortDistribution, bool>>>(expr =>
                expr.Compile().Invoke(new CohortDistribution { NHSNumber = _serviceNowParticipant.NhsNumber })
            ))).ReturnsAsync(existingCohortDistribution);

        // Act
        await _function.Run(_serviceNowParticipant);

        // Assert
        _cohortDistributionClientMock.Verify(x => x.GetSingleByFilter(It.IsAny<Expression<Func<CohortDistribution, bool>>>()), Times.Once);
        _cohortDistributionClientMock.Verify(x => x.Update(It.IsAny<CohortDistribution>()), Times.Never);
        _loggerMock.VerifyLogger(LogLevel.Information, $"Primary_Care_Provider for NHS Number: {_serviceNowParticipant.NhsNumber} is already up to date: REAL123");
    }

    [TestMethod]
    [DataRow("ZZZ456", "REAL123")]
    [DataRow("OLD789", "NEW123")]
    [DataRow("", "NEW123")]
    public async Task Run_WhenAmendingExistingParticipant_UpdatesCohortDistributionWhenGpCodeChanges(string currentGpCode, string pdsGpCode)
    {
        // Arrange
        SetupPdsResponse(_serviceNowParticipant, pdsGpCode);

        var existingParticipant = new ParticipantManagement
        {
            ParticipantId = 123,
            ScreeningId = _serviceNowParticipant.ScreeningId,
            NHSNumber = _serviceNowParticipant.NhsNumber
        };

        _dataServiceClientMock.Setup(client => client.GetSingleByFilter(
            It.Is<Expression<Func<ParticipantManagement, bool>>>(x =>
                x.Compile().Invoke(new ParticipantManagement { NHSNumber = _serviceNowParticipant.NhsNumber, ScreeningId = _serviceNowParticipant.ScreeningId })
            ))).ReturnsAsync(existingParticipant);

        _dataServiceClientMock.Setup(x => x.Update(It.IsAny<ParticipantManagement>())).ReturnsAsync(true);

        var existingCohortDistribution = new CohortDistribution
        {
            NHSNumber = _serviceNowParticipant.NhsNumber,
            PrimaryCareProvider = currentGpCode
        };

        _cohortDistributionClientMock.Setup(x => x.GetSingleByFilter(
            It.Is<Expression<Func<CohortDistribution, bool>>>(expr =>
                expr.Compile().Invoke(new CohortDistribution { NHSNumber = _serviceNowParticipant.NhsNumber })
            ))).ReturnsAsync(existingCohortDistribution);

        _cohortDistributionClientMock.Setup(x => x.Update(It.Is<CohortDistribution>(c =>
            c.NHSNumber == _serviceNowParticipant.NhsNumber &&
            c.PrimaryCareProvider == pdsGpCode &&
            c.RecordUpdateDateTime != null))).ReturnsAsync(true);

        // Act
        await _function.Run(_serviceNowParticipant);

        // Assert
        _cohortDistributionClientMock.Verify(x => x.GetSingleByFilter(It.IsAny<Expression<Func<CohortDistribution, bool>>>()), Times.Once);
        _cohortDistributionClientMock.Verify(x => x.Update(It.IsAny<CohortDistribution>()), Times.Once);
        _loggerMock.VerifyLogger(LogLevel.Information, $"AMEND participant with NHS Number: {_serviceNowParticipant.NhsNumber}, overwriting Primary_Care_Provider with PDS data: {pdsGpCode}");
        _loggerMock.VerifyLogger(LogLevel.Information, $"Successfully updated Primary Care Provider in Cohort Distribution for NHS Number: {_serviceNowParticipant.NhsNumber}");
    }

    [TestMethod]
    public async Task Run_WhenAmendingParticipantButNoPdsGpCode_DoesNotUpdateCohortDistribution()
    {
        // Arrange
        SetupPdsResponse(_serviceNowParticipant, "");

        var existingParticipant = new ParticipantManagement
        {
            ParticipantId = 123,
            ScreeningId = _serviceNowParticipant.ScreeningId,
            NHSNumber = _serviceNowParticipant.NhsNumber
        };

        _dataServiceClientMock.Setup(client => client.GetSingleByFilter(
            It.Is<Expression<Func<ParticipantManagement, bool>>>(x =>
                x.Compile().Invoke(new ParticipantManagement { NHSNumber = _serviceNowParticipant.NhsNumber, ScreeningId = _serviceNowParticipant.ScreeningId })
            ))).ReturnsAsync(existingParticipant);

        _dataServiceClientMock.Setup(x => x.Update(It.IsAny<ParticipantManagement>())).ReturnsAsync(true);

        // Act
        await _function.Run(_serviceNowParticipant);

        // Assert
        _cohortDistributionClientMock.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task Run_WhenCohortDistributionUpdateFails_LogsError()
    {
        // Arrange
        var participantWithDummyGp = new ServiceNowParticipant()
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

        SetupPdsResponse(participantWithDummyGp);

        _dataServiceClientMock.Setup(client => client.GetSingleByFilter(It.IsAny<Expression<Func<ParticipantManagement, bool>>>()))
            .ReturnsAsync((ParticipantManagement)null!);
        _dataServiceClientMock.Setup(x => x.Add(It.IsAny<ParticipantManagement>())).ReturnsAsync(true);

        var existingCohortDistribution = new CohortDistribution { NHSNumber = participantWithDummyGp.NhsNumber };
        _cohortDistributionClientMock.Setup(x => x.GetSingleByFilter(It.IsAny<Expression<Func<CohortDistribution, bool>>>()))
            .ReturnsAsync(existingCohortDistribution);
        _cohortDistributionClientMock.Setup(x => x.Update(It.IsAny<CohortDistribution>())).ReturnsAsync(false);

        // Act
        await _function.Run(participantWithDummyGp);

        // Assert
        _loggerMock.VerifyLogger(LogLevel.Error, $"Failed to update Primary Care Provider in Cohort Distribution for NHS Number: {participantWithDummyGp.NhsNumber}");
    }

    private void SetupPdsResponse(ServiceNowParticipant participant, string? primaryCareProvider = null)
    {
        var json = JsonSerializer.Serialize(new ParticipantDemographic
        {
            NhsNumber = participant.NhsNumber,
            GivenName = participant.FirstName,
            FamilyName = participant.FamilyName,
            DateOfBirth = participant.DateOfBirth.ToString("yyyy-MM-dd"),
            PrimaryCareProvider = primaryCareProvider
        });

        _httpClientFunctionMock.Setup(x => x.SendGetResponse($"{_configMock.Object.Value.RetrievePdsDemographicURL}?nhsNumber={participant.NhsNumber}"))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
    }
}
