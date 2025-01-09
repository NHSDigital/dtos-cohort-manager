namespace NHS.CohortManager.Tests.UnitTests.ParticipantManagementServiceTests;

using Azure;
using System.Linq.Expressions;
using Microsoft.Extensions.Logging;
using Common;
using Model;
using Moq;
using NHS.CohortManager.ParticipantManagementServices;
using Azure.Messaging.EventGrid;
using DataServices.Client;

[TestClass]
public class UpdateParticipantFromScreeningProviderTests
{
    private readonly Mock<ILogger<UpdateParticipantFromScreeningProvider>> _loggerMock = new();
    private readonly Mock<IExceptionHandler> _handleExceptionMock = new();
    private readonly Mock<IDataServiceClient<ParticipantManagement>> _participantManagementDataServiceMock = new();
    private readonly Mock<IDataServiceClient<GeneCodeLkp>> _geneCodeDataServiceMock = new();
    private readonly Mock<IDataServiceClient<HigherRiskReferralReasonLkp>> _higherRiskReferralDataServiceMock = new();
    private readonly Mock<EventGridPublisherClient> _eventGridPublisherClientMock = new();
    private readonly Mock<Response> _eventGridResponseMock = new();
    private readonly BiAnalyticsParticipantDto _reqParticipant;
    private readonly UpdateParticipantFromScreeningProvider _sut;

    public UpdateParticipantFromScreeningProviderTests()
    {
        _reqParticipant = new BiAnalyticsParticipantDto
        {
            NhsNumber = 1234,
            ScreeningId = 1,
            GeneCode = "BRCA1",
            HigherRiskReferralReasonCode = "AT_HETEROZYGOTES",
            SrcSysProcessedDateTime = DateTime.Now,

        };

        var dbParticipant = new ParticipantManagement
        {
            ParticipantId = 1,
            NHSNumber = 1234,
            ScreeningId = 1,
            GeneCodeId = 1,
            HigherRiskReferralReasonId = 2,
            IsHigherRisk = 0,
            IsHigherRiskActive = 0,
            SrcSysProcessedDateTime = DateTime.Now.AddDays(-1)
        };

        _handleExceptionMock
            .Setup(x => x.CreateSystemExceptionLogFromNhsNumber(
                It.IsAny<Exception>(),
                It.IsAny<String>(),
                It.IsAny<String>(),
                It.IsAny<String>(),
                It.IsAny<String>()))
            .Returns(Task.CompletedTask);

        _participantManagementDataServiceMock
            .Setup(x => x.GetSingleByFilter(It.IsAny<Expression<Func<ParticipantManagement, bool>>>()))
            .ReturnsAsync(dbParticipant);

        _participantManagementDataServiceMock
            .Setup(x => x.Update(It.IsAny<ParticipantManagement>()))
            .ReturnsAsync(true);

        _geneCodeDataServiceMock
            .Setup(x => x.GetSingleByFilter(It.IsAny<Expression<Func<GeneCodeLkp, bool>>>()))
            .ReturnsAsync(new GeneCodeLkp {GeneCodeId = 0000});

        _higherRiskReferralDataServiceMock
            .Setup(x => x.GetSingleByFilter(It.IsAny<Expression<Func<HigherRiskReferralReasonLkp, bool>>>()))
            .ReturnsAsync(new HigherRiskReferralReasonLkp {HigherRiskReferralReasonId = 1111});

        _eventGridResponseMock
            .Setup(x => x.Status)
            .Returns(200);
        _eventGridPublisherClientMock
            .Setup(x => x.SendEventAsync(It.IsAny<EventGridEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_eventGridResponseMock.Object);

        _sut = new UpdateParticipantFromScreeningProvider(_loggerMock.Object, _participantManagementDataServiceMock.Object,
                                                        _higherRiskReferralDataServiceMock.Object, _geneCodeDataServiceMock.Object,
                                                        _eventGridPublisherClientMock.Object, _handleExceptionMock.Object);
    }

    [TestMethod]
    public async Task Run_ValidRequest_SendSuccessEvent()
    {
        // Arrange
        var message = new EventGridEvent(
            subject: "IDK",
            eventType: "IDK",
            dataVersion: "1.0",
            data: _reqParticipant
        );

        // Act
        await _sut.Run(message);

        // Assert
        _eventGridPublisherClientMock
            .Verify(x => x.SendEventAsync(
                It.Is<EventGridEvent>(e => e.EventType == "Success"),
                It.IsAny<CancellationToken>()
            ));
    }

    [TestMethod]
    public async Task Run_ParticipantDoesNotExist_ThrowKeyNotFoundException()
    {
        // Arrange
        _participantManagementDataServiceMock
            .Setup(x => x.GetSingleByFilter(It.IsAny<Expression<Func<ParticipantManagement, bool>>>()))
            .ReturnsAsync((ParticipantManagement)null);

        var message = new EventGridEvent(
            subject: "IDK",
            eventType: "IDK",
            dataVersion: "1.0",
            data: _reqParticipant
        );

        // Act
        await _sut.Run(message);

        // Assert
        _handleExceptionMock
            .Verify(x => x.CreateSystemExceptionLogFromNhsNumber(
                It.Is<KeyNotFoundException>(ex => ex.Message == "Participant not found"),
                It.IsAny<string>(),
                It.IsAny<String>(),
                It.IsAny<String>(),
                It.IsAny<String>()),
            Times.Once);
    }

    [TestMethod]
    public async Task Run_ParticipantUpdateFails_ThrowIOException()
    {
        // Arrange
        _participantManagementDataServiceMock
            .Setup(x => x.Update(It.IsAny<ParticipantManagement>()))
            .ReturnsAsync(false);

        var message = new EventGridEvent(
            subject: "IDK",
            eventType: "IDK",
            dataVersion: "1.0",
            data: _reqParticipant
        );

        // Act
        await _sut.Run(message);

        // Assert
        _handleExceptionMock
            .Verify(x => x.CreateSystemExceptionLogFromNhsNumber(
                It.Is<IOException>(ex => ex.Message == "Updating participant management object failed"),
                It.IsAny<string>(),
                It.IsAny<String>(),
                It.IsAny<String>(),
                It.IsAny<String>()),
            Times.Once);
    }

    [TestMethod]
    public async Task Run_RequestDataIsOlder_ThrowInvalidOperationException()
    {
        // Arrange
        _reqParticipant.SrcSysProcessedDateTime = DateTime.Now.AddDays(-2);
        var message = new EventGridEvent(
            subject: "IDK",
            eventType: "IDK",
            dataVersion: "1.0",
            data: _reqParticipant
        );

        // Act
        await _sut.Run(message);

        // Assert
        _handleExceptionMock
            .Verify(x => x.CreateSystemExceptionLogFromNhsNumber(
                It.Is<InvalidOperationException>(ex => ex.Message == "Request participant data is older than database participant data"),
                It.IsAny<string>(),
                It.IsAny<String>(),
                It.IsAny<String>(),
                It.IsAny<String>()),
            Times.Once);
    }

    [TestMethod]
    public async Task Run_ReferenceDataLookupFailed_ThrowException()
    {
        // Arrange
        _geneCodeDataServiceMock
            .Setup(x => x.GetSingleByFilter(It.IsAny<Expression<Func<GeneCodeLkp, bool>>>()))
            .ReturnsAsync((GeneCodeLkp) null);

        var message = new EventGridEvent(
            subject: "IDK",
            eventType: "IDK",
            dataVersion: "1.0",
            data: _reqParticipant
        );

        // Act
        await _sut.Run(message);

        // Assert
        _handleExceptionMock
            .Verify(x => x.CreateSystemExceptionLogFromNhsNumber(
                It.IsAny<Exception>(),
                It.IsAny<string>(),
                It.IsAny<String>(),
                It.IsAny<String>(),
                It.IsAny<String>()),
            Times.Once);
    }

    [TestMethod]
    public async Task Run_SendToEventGridFails_ThrowIOException()
    {
        // Arrange
        _eventGridResponseMock
            .Setup(x => x.Status)
            .Returns(500);
        _eventGridPublisherClientMock
            .Setup(x => x.SendEventAsync(It.IsAny<EventGridEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_eventGridResponseMock.Object);

        var message = new EventGridEvent(
            subject: "IDK",
            eventType: "IDK",
            dataVersion: "1.0",
            data: _reqParticipant
        );

        // Act
        await _sut.Run(message);

        // Assert
        _handleExceptionMock
            .Verify(x => x.CreateSystemExceptionLogFromNhsNumber(
                It.Is<IOException>(ex => ex.Message == "Sending event failed"),
                It.IsAny<string>(),
                It.IsAny<String>(),
                It.IsAny<String>(),
                It.IsAny<String>()),
            Times.Once);
    }
}
