namespace NHS.CohortManager.Tests.PdsProcessorTests;

using System.Text.Json;
using Common;
using DataServices.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Model;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using Moq;
using NHS.CohortManager.DemographicServices;

[TestClass]
public class PdsProcessorTests
{
    private readonly Mock<ILogger<PdsProcessor>> _mockLogger = new();
    private readonly Mock<ICreateBasicParticipantData> _mockCreateBasicParticipantData = new();
    private readonly Mock<IDataServiceClient<ParticipantDemographic>> _dataServiceClient = new();
    private readonly Mock<IAddBatchToQueue> _addBatchToQueue = new();
    private readonly Mock<IOptions<RetrievePDSDemographicConfig>> _retrievePDSDemographicConfig = new();
    private readonly RetrievePDSDemographicConfig _testConfig;
    private readonly string nhsNumber = "1111110662";
    private readonly PdsProcessor _pdsProcessor;

    public PdsProcessorTests()
    {
        _testConfig = new RetrievePDSDemographicConfig
        {
            RetrievePdsParticipantURL = "",
            DemographicDataServiceURL = "",
            Audience = "",
            KId = "",
            AuthTokenURL = "",
            ParticipantManagementTopic = "some-fake-topic",
            ServiceBusConnectionString_client_internal = "",
            UseFakePDSServices = false
        };

        _mockCreateBasicParticipantData.Setup(x => x.BasicParticipantData(It.IsAny<Participant>()))
            .Returns(new BasicParticipantData());

        _retrievePDSDemographicConfig.Setup(x => x.Value).Returns(_testConfig);

        _dataServiceClient.Setup(x => x.Update(It.IsAny<ParticipantDemographic>())).ReturnsAsync(true);
        _dataServiceClient.Setup(x => x.Add(It.IsAny<ParticipantDemographic>())).ReturnsAsync(true);
        _pdsProcessor = new PdsProcessor(_mockLogger.Object, _mockCreateBasicParticipantData.Object, _dataServiceClient.Object, _addBatchToQueue.Object, _retrievePDSDemographicConfig.Object);
    }

    [TestMethod]
    public async Task ProcessPdsNotFoundResponse_WhenResponseContainsInvalidatedResourceCodeAndUpdateIsFromNems_SendsRemovalToQueue()
    {
        // Arrange
        var sourceFileName = "nems-file-123.xml";
        var httpResponseMessage = CreatePdsErrorResponse(PdsConstants.InvalidatedResourceCode);

        // Act
        await _pdsProcessor.ProcessPdsNotFoundResponse(httpResponseMessage, nhsNumber, sourceFileName);

        // Assert
        _mockLogger.Verify(x => x.Log(It.Is<LogLevel>(l => l == LogLevel.Information),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("NotFound response contains INVALIDATED_RESOURCE code")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception, string>>()),
        Times.Once);

        _addBatchToQueue.Verify(x => x.ProcessBatch(
                It.Is<ConcurrentQueue<BasicParticipantCsvRecord>>(
                    x => x.Count == 1 &&
                    x.First().FileName == sourceFileName &&
                    x.First().Participant.NhsNumber == nhsNumber &&
                    x.First().Participant.PrimaryCareProvider == null &&
                    x.First().Participant.ReasonForRemoval == PdsConstants.OrrRemovalReason &&
                    x.First().Participant.ReasonForRemovalEffectiveFromDate == DateTime.UtcNow.Date.ToString("yyyyMMdd")),
                _testConfig.ParticipantManagementTopic),
            Times.Once);
    }

    [TestMethod]
    public async Task ProcessPdsNotFoundResponse_WhenResponseContainsInvalidatedResourceCodeAndUpdateIsNotFromNems_DoesNotSendRemovalToQueue()
    {
        // Arrange
        var httpResponseMessage = CreatePdsErrorResponse(PdsConstants.InvalidatedResourceCode);

        // Act
        await _pdsProcessor.ProcessPdsNotFoundResponse(httpResponseMessage, nhsNumber);

        // Assert
        _addBatchToQueue.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task ProcessPdsNotFoundResponse_WhenResponseDoesNotContainInvalidatedResourceCodeAndUpdatesIsFromNems_DoesNotSendRemovalToQueue()
    {
        // Arrange
        var sourceFileName = "nems-file-123.xml";
        var httpResponseMessage = CreatePdsErrorResponse("TEST_CODE");

        // Act
        await _pdsProcessor.ProcessPdsNotFoundResponse(httpResponseMessage, nhsNumber, sourceFileName);

        // Assert
        _addBatchToQueue.VerifyNoOtherCalls();
    }


    [TestMethod]
    public async Task UpsertDemographicRecordFromPDS_WithExistingRecord_ReturnsTrue()
    {
        _dataServiceClient.Setup(x => x.GetSingleByFilter(It.IsAny<System.Linq.Expressions.Expression<Func<ParticipantDemographic, bool>>>())).ReturnsAsync(new ParticipantDemographic()
        {
            NhsNumber = 1111110662
        });

        var res = await _pdsProcessor.UpsertDemographicRecordFromPDS(new ParticipantDemographic());

        _mockLogger.Verify(x => x.Log(It.Is<LogLevel>(l => l == LogLevel.Information),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Successfully updated Participant Demographic.")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);

        _mockLogger.Verify(x => x.Log(It.Is<LogLevel>(l => l == LogLevel.Information),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Participant Demographic record found, attempting to update Participant Demographic.")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);

        Assert.IsTrue(res);
    }

    [TestMethod]
    public async Task UpsertDemographicRecordFromPDS_WithNewRecord_ReturnsTrue()
    {
        var res = await _pdsProcessor.UpsertDemographicRecordFromPDS(new ParticipantDemographic());

        _mockLogger.Verify(x => x.Log(It.Is<LogLevel>(l => l == LogLevel.Information),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Successfully updated Participant Demographic.")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Never);

        _mockLogger.Verify(x => x.Log(It.Is<LogLevel>(l => l == LogLevel.Information),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Participant Demographic record found, attempting to update Participant Demographic.")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Never);

        _mockLogger.Verify(x => x.Log(It.Is<LogLevel>(l => l == LogLevel.Information),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Successfully added Participant Demographic.")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);

        Assert.IsTrue(res);
    }

    [TestMethod]
    public async Task UpsertDemographicRecordFromPDS_FailsToAddNewRecord_ReturnsFalse()
    {
        _dataServiceClient.Setup(x => x.Add(It.IsAny<ParticipantDemographic>())).ReturnsAsync(false);
        var res = await _pdsProcessor.UpsertDemographicRecordFromPDS(new ParticipantDemographic());

        _mockLogger.Verify(x => x.Log(It.Is<LogLevel>(l => l == LogLevel.Information),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Successfully updated Participant Demographic.")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Never);

        _mockLogger.Verify(x => x.Log(It.Is<LogLevel>(l => l == LogLevel.Information),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Participant Demographic record found, attempting to update Participant Demographic.")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Never);

        _mockLogger.Verify(x => x.Log(It.Is<LogLevel>(l => l == LogLevel.Information),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Successfully added Participant Demographic.")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Never);

        Assert.IsFalse(res);
    }

    [TestMethod]
    public async Task UpsertDemographicRecordFromPDS_UpdateRecordFails_ReturnsFalse()
    {
        _dataServiceClient.Setup(x => x.GetSingleByFilter(It.IsAny<System.Linq.Expressions.Expression<Func<ParticipantDemographic, bool>>>())).ReturnsAsync(new ParticipantDemographic()
        {
            NhsNumber = 1111110662
        });
        _dataServiceClient.Setup(x => x.Update(It.IsAny<ParticipantDemographic>())).ReturnsAsync(false);


        var res = await _pdsProcessor.UpsertDemographicRecordFromPDS(new ParticipantDemographic());

        _mockLogger.Verify(x => x.Log(It.Is<LogLevel>(l => l == LogLevel.Information),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Successfully updated Participant Demographic.")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Never);

        _mockLogger.Verify(x => x.Log(It.Is<LogLevel>(l => l == LogLevel.Information),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Participant Demographic record found, attempting to update Participant Demographic.")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);

        _mockLogger.Verify(x => x.Log(It.Is<LogLevel>(l => l == LogLevel.Error),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to update Participant Demographic.")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);


        Assert.IsFalse(res);
    }

    private static HttpResponseMessage CreatePdsErrorResponse(string code)
    {
        var errorResponse = new PdsErrorResponse()
        {
            issue = new List<PdsIssue>
            {
                new PdsIssue
                {
                    code = string.Empty,
                    details = new PdsErrorDetails
                    {
                        coding = new List<PdsCoding>
                        {
                            new PdsCoding { code = code }
                        }
                    }
                }
            }
        };

        var msg = new HttpResponseMessage
        {
            Content = new StringContent(JsonSerializer.Serialize(errorResponse))
        };
        return msg;
    }
}
