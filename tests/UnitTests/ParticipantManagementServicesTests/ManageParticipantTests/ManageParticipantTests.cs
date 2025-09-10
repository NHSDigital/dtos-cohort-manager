namespace NHS.CohortManager.Tests.ParticipantManagementServiceTests;

using Common;
using Microsoft.Extensions.Logging;
using System.Net;
using Moq;
using System.Text.Json;
using Model;
using Microsoft.Extensions.Options;
using NHS.CohortManager.ParticipantManagementServices;
using DataServices.Client;
using System.Linq.Expressions;
using Azure.Core;
using Model.Enums;

[TestClass]
public class ManageParticipantTests
{
    private readonly Mock<ILogger<ManageParticipant>> _loggerMock = new();
    private readonly Mock<IExceptionHandler> _handleException = new();
    private readonly Mock<IOptions<ManageParticipantConfig>> _config = new();
    private readonly Mock<IDataServiceClient<ParticipantManagement>> _participantManagementClientMock = new();
    private readonly Mock<IQueueClient> _queueClientMock = new();
    private readonly ManageParticipant _sut;
    private readonly BasicParticipantCsvRecord _request;

    public ManageParticipantTests()
    {
        var testConfig = new ManageParticipantConfig
        {
            CohortDistributionTopic = "CohortTopicName"
        };

        _config.Setup(c => c.Value).Returns(testConfig);

        _participantManagementClientMock
            .Setup(x => x.Add(It.IsAny<ParticipantManagement>()))
            .ReturnsAsync(true);

        _participantManagementClientMock
            .Setup(x => x.Update(It.IsAny<ParticipantManagement>()))
            .ReturnsAsync(true);

        _participantManagementClientMock
            .Setup(x => x.GetSingleByFilter(It.IsAny<Expression<Func<ParticipantManagement, bool>>>()))
            .ReturnsAsync((ParticipantManagement)null);

        _queueClientMock
            .Setup(x => x.AddAsync(It.IsAny<ParticipantCsvRecord>(), testConfig.CohortDistributionTopic))
            .ReturnsAsync(true);

        _sut = new ManageParticipant(
            _loggerMock.Object,
            _config.Object,
            _queueClientMock.Object,
            _participantManagementClientMock.Object,
            _handleException.Object
        );

        _request = new BasicParticipantCsvRecord
        {
            FileName = "mockFileName",
            Participant = new Participant
            {
                NhsNumber = "9444567877",
                ScreeningName = "mockScreeningName",
                SupersededByNhsNumber = "789012",
                PrimaryCareProvider = "ABC Clinic",
                NamePrefix = "Mr.",
                FirstName = "John",
                OtherGivenNames = "Middle",
                FamilyName = "Doe",
                DateOfBirth = "1990-01-01",
                Gender = Gender.Male,
                AddressLine1 = "123 Main Street",
                AddressLine2 = "Apt 101",
                AddressLine3 = "Suburb",
                AddressLine4 = "City",
                AddressLine5 = "State",
                Postcode = "12345",
                ReasonForRemoval = "Moved",
                ReasonForRemovalEffectiveFromDate = "2024-04-23",
                DateOfDeath = "2024-04",
                TelephoneNumber = "123-456-7890",
                MobileNumber = "987-654-3210",
                EmailAddress = "john.doe@example.com",
                PreferredLanguage = "English",
                IsInterpreterRequired = "0",
                RecordType = Actions.Amended,
                ScreeningId = "1",
                ReferralFlag = "0"
            }
        };

    }

    [TestMethod]
    public async Task Run_ParticipantNotInTable_AddParticipantAndSendToQueue()
    {
        // Act
        await _sut.Run(JsonSerializer.Serialize(_request));

        // Assert
        _participantManagementClientMock
            .Verify(x => x.Add(It.Is<ParticipantManagement>(p => p.RecordInsertDateTime != null)), Times.Once);
        _participantManagementClientMock
            .Verify(x => x.Update(It.IsAny<ParticipantManagement>()), Times.Never);
        _queueClientMock
            .Verify(x => x.AddAsync(It.IsAny<BasicParticipantCsvRecord>(), It.IsAny<string>()), Times.Once);
    }

    [TestMethod]
    public async Task Run_ParticipantInTable_UpdateParticipantAndSendToQueue()
    {
        // Arrange
        _participantManagementClientMock
            .Setup(x => x.GetSingleByFilter(It.IsAny<Expression<Func<ParticipantManagement, bool>>>()))
            .ReturnsAsync(new ParticipantManagement());

        // Act
        await _sut.Run(JsonSerializer.Serialize(_request));

        // Assert
        _participantManagementClientMock
            .Verify(x => x.Update(It.Is<ParticipantManagement>(p => p.RecordUpdateDateTime != null)), Times.Once);
        _participantManagementClientMock
            .Verify(x => x.Add(It.IsAny<ParticipantManagement>()), Times.Never);
        _queueClientMock
            .Verify(x => x.AddAsync(It.IsAny<BasicParticipantCsvRecord>(), It.IsAny<string>()), Times.Once);
    }

    [TestMethod]
    public async Task Run_InvalidNhsNumber_CreateExceptionAndReturn()
    {
        // Arrange
        _request.Participant.NhsNumber = "12345";

        // Act
        await _sut.Run(JsonSerializer.Serialize(_request));

        // Assert
        _participantManagementClientMock
            .Verify(x => x.Add(It.IsAny<ParticipantManagement>()), Times.Never);
        _participantManagementClientMock
            .Verify(x => x.Update(It.IsAny<ParticipantManagement>()), Times.Never);
        _queueClientMock
            .Verify(x => x.AddAsync(It.IsAny<BasicParticipantCsvRecord>(), It.IsAny<string>()), Times.Never);
        _handleException
            .Verify(i => i.CreateSystemExceptionLog(
                It.IsAny<ArgumentException>(),
                It.IsAny<Participant>(),
                It.IsAny<string>(),
                ""), Times.Once);
    }

    [TestMethod]
    public async Task Run_ParticipantIsBlocked_CreateExceptionAndReturn()
    {
        // Arrange
        _participantManagementClientMock
            .Setup(x => x.GetSingleByFilter(It.IsAny<Expression<Func<ParticipantManagement, bool>>>()))
            .ReturnsAsync(new ParticipantManagement { BlockedFlag = 1 });

        // Act
        await _sut.Run(JsonSerializer.Serialize(_request));

        // Assert

        //__handleException.CreateSystemExceptionLog(ex, participant, fileName, category);
        _participantManagementClientMock
            .Verify(x => x.Add(It.IsAny<ParticipantManagement>()), Times.Never);
        _participantManagementClientMock
            .Verify(x => x.Update(It.IsAny<ParticipantManagement>()), Times.Never);
        _queueClientMock
            .Verify(x => x.AddAsync(It.IsAny<BasicParticipantCsvRecord>(), It.IsAny<string>()), Times.Never);
        _handleException
            .Verify(i => i.CreateSystemExceptionLog(
                It.IsAny<InvalidOperationException>(),
                It.IsAny<Participant>(),
                It.IsAny<string>(),
                "0"), Times.Once);
    }

    [TestMethod]
    public async Task Run_AddFails_CreateExceptionAndReturn()
    {
        // Arrange
        _participantManagementClientMock
            .Setup(x => x.Add(It.IsAny<ParticipantManagement>()))
            .ReturnsAsync(false);

        // Act
        await _sut.Run(JsonSerializer.Serialize(_request));

        // Assert
        _participantManagementClientMock
            .Verify(x => x.Add(It.IsAny<ParticipantManagement>()), Times.Once);
        _participantManagementClientMock
            .Verify(x => x.Update(It.IsAny<ParticipantManagement>()), Times.Never);
        _queueClientMock
            .Verify(x => x.AddAsync(It.IsAny<BasicParticipantCsvRecord>(), It.IsAny<string>()), Times.Never);
        _handleException
            .Verify(i => i.CreateSystemExceptionLog(
                It.IsAny<InvalidOperationException>(),
                It.IsAny<Participant>(),
                It.IsAny<string>(),
                ""), Times.Once);
    }

    [TestMethod]
    public async Task Run_DatabaseAndRequestReferralFlagsDifferent_UseDatabaseFlag()
    {
        // Arrange
        ParticipantManagement dbParticipant = new()
        {
            NHSNumber = 9444567877,
            ReferralFlag = 1
        };

        _participantManagementClientMock
            .Setup(x => x.GetSingleByFilter(It.IsAny<Expression<Func<ParticipantManagement, bool>>>()))
            .ReturnsAsync(dbParticipant);

        // Act
        await _sut.Run(JsonSerializer.Serialize(_request));

        // Assert
        _participantManagementClientMock
            .Verify(x => x.Update(It.Is<ParticipantManagement>(x => x.ReferralFlag == 1)), Times.Once);
    }
}
