namespace NHS.CohortManager.Tests.PdsProcessorTests;

using System.Text.Json;
using Common;
using DataServices.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Model;
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
    string nhsNumber = "1111110662";

    private readonly PdsProcessor _pdsProcessor;
    public PdsProcessorTests()
    {
        var testConfig = new RetrievePDSDemographicConfig
        {
            RetrievePdsParticipantURL = "",
            DemographicDataServiceURL = "",
            Audience = "",
            KId = "",
            AuthTokenURL = "",
            ParticipantManagementTopic = "some-fake-topic",
            ServiceBusConnectionString = "",
            UseFakePDSServices = false
        };


        _mockCreateBasicParticipantData.Setup(x => x.BasicParticipantData(It.IsAny<Participant>()))
       .Returns(new BasicParticipantData());

        _retrievePDSDemographicConfig.Setup(x => x.Value).Returns(testConfig);

        _dataServiceClient.Setup(x => x.Update(It.IsAny<ParticipantDemographic>())).ReturnsAsync(true);
        _dataServiceClient.Setup(x => x.Add(It.IsAny<ParticipantDemographic>())).ReturnsAsync(true);
        _pdsProcessor = new PdsProcessor(_mockLogger.Object, _mockCreateBasicParticipantData.Object, _dataServiceClient.Object, _addBatchToQueue.Object, _retrievePDSDemographicConfig.Object);

    }

    [TestMethod]
    public async Task ProcessPdsNotFoundResponse_ProcessesResponse_SendsForDistribution()
    {
        var errorResponse = new PdsErrorResponse()
        {
            issue = new List<PdsIssue>()
            {
                new PdsIssue()
                {
                    code = "",
                    details = new PdsErrorDetails()
                    {
                        coding = new List<PdsCoding>()
                        {
                            new PdsCoding()
                            {
                                code = "INVALIDATED_RESOURCE"
                            }
                        }
                    }
                }
            }
        };

        HttpResponseMessage httpResponseMessage = new HttpResponseMessage();
        httpResponseMessage.Content = new StringContent(JsonSerializer.Serialize(errorResponse));

        await _pdsProcessor.ProcessPdsNotFoundResponse(httpResponseMessage, nhsNumber);

        _mockLogger.Verify(x => x.Log(It.Is<LogLevel>(l => l == LogLevel.Information),
             It.IsAny<EventId>(),
             It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"Sending record to the update queue.")),
             It.IsAny<Exception>(),
             It.IsAny<Func<It.IsAnyType, Exception, string>>()),
         Times.Once);

        _mockLogger.Verify(x => x.Log(It.Is<LogLevel>(l => l == LogLevel.Error),
           It.IsAny<EventId>(),
           It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"the PDS function has returned a 404 error. function now stopping processing")),
           It.IsAny<Exception>(),
           It.IsAny<Func<It.IsAnyType, Exception, string>>()),
       Times.Never);
    }

    [TestMethod]
    public async Task ProcessPdsNotFoundResponse_WithNonInvalidatedResource_LogsError()
    {
        var errorResponse = new PdsErrorResponse()
        {
            issue = new List<PdsIssue>()
            {
                new PdsIssue()
                {
                    code = "",
                    details = new PdsErrorDetails()
                    {
                        coding = new List<PdsCoding>()
                        {
                            new PdsCoding()
                            {
                                code = ""
                            }
                        }
                    }
                }
            }
        };

        HttpResponseMessage httpResponseMessage = new HttpResponseMessage();
        httpResponseMessage.Content = new StringContent(JsonSerializer.Serialize(errorResponse));

        await _pdsProcessor.ProcessPdsNotFoundResponse(httpResponseMessage, nhsNumber);

        _mockLogger.Verify(x => x.Log(It.Is<LogLevel>(l => l == LogLevel.Information),
             It.IsAny<EventId>(),
             It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"Sending record to the update queue.")),
             It.IsAny<Exception>(),
             It.IsAny<Func<It.IsAnyType, Exception, string>>()),
         Times.Never);

        _mockLogger.Verify(x => x.Log(It.Is<LogLevel>(l => l == LogLevel.Error),
           It.IsAny<EventId>(),
           It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"the PDS function has returned a 404 error. function now stopping processing")),
           It.IsAny<Exception>(),
           It.IsAny<Func<It.IsAnyType, Exception, string>>()),
       Times.Once);
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


}