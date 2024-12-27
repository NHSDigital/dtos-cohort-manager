namespace NHS.CohortManager.Tests.CaasIntegrationTests;

using System.Diagnostics;
using System.Text.Json;
using Common;
using Common.Interfaces;
using Microsoft.Extensions.Logging;
using Model;
using Moq;
using NHS.Screening.ReceiveCaasFile;
using receiveCaasFile;

[TestClass]
public class ProcessCaasFileTests
{
    private Mock<ILogger<ProcessCaasFile>> _loggerMock;
    private Mock<ICallFunction> _callFunctionMock;
    private Mock<IReceiveCaasFileHelper> _receiveCaasFileHelperMock;
    private Mock<ICheckDemographic> _checkDemographicMock;
    private Mock<ICreateBasicParticipantData> _createBasicParticipantDataMock;
    private Mock<IExceptionHandler> _exceptionHandlerMock;
    private Mock<IAddBatchToQueue> _addBatchToQueueMock;
    private Mock<RecordsProcessedTracker> _recordsProcessedTrackerMock;
    private ProcessCaasFile _processCaasFile;

    public ProcessCaasFileTests()
    {
        _loggerMock = new Mock<ILogger<ProcessCaasFile>>();
        _callFunctionMock = new Mock<ICallFunction>();
        _receiveCaasFileHelperMock = new Mock<IReceiveCaasFileHelper>();
        _checkDemographicMock = new Mock<ICheckDemographic>();
        _createBasicParticipantDataMock = new Mock<ICreateBasicParticipantData>();
        _exceptionHandlerMock = new Mock<IExceptionHandler>();
        _addBatchToQueueMock = new Mock<IAddBatchToQueue>();
        _recordsProcessedTrackerMock = new Mock<RecordsProcessedTracker>();

        _processCaasFile = new ProcessCaasFile(
            _loggerMock.Object,
            _callFunctionMock.Object,
            _checkDemographicMock.Object,
            _createBasicParticipantDataMock.Object,
            _exceptionHandlerMock.Object,
            _addBatchToQueueMock.Object,
            _receiveCaasFileHelperMock.Object,
            _exceptionHandlerMock.Object,
            _recordsProcessedTrackerMock.Object
        );
    }

    // could try and test weather or not a batch has been processed in certain ways depending on what record type we get
    // make sure demographic function has been called 
    // check to make sure certain exceptions hav been raised according to date

    [TestMethod]
    public async Task ProcessRecords_ValidParticipants_ProcessesSuccessfully()
    {
        // Arrange
        var participants = new List<ParticipantsParquetMap>
            {
                new ParticipantsParquetMap { NhsNumber = 1234567890 },
                new ParticipantsParquetMap { NhsNumber = 9876543210 }
            };
        var options = new ParallelOptions();
        var screeningService = new ScreeningService { ScreeningId = "1", ScreeningName = "Test Screening" };
        const string fileName = "TestFile";

        _receiveCaasFileHelperMock.Setup(helper => helper.MapParticipant(It.IsAny<ParticipantsParquetMap>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new Participant { NhsNumber = "1234567890", RecordType = Actions.New });

        _checkDemographicMock.Setup(demo => demo.PostDemographicDataAsync(It.IsAny<Participant>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        // Act
        await _processCaasFile.ProcessRecords(participants, options, screeningService, fileName);

        // Assert
        _addBatchToQueueMock.Verify(queue => queue.ProcessBatch(It.IsAny<Batch>()), Times.Once);
    }

    [TestMethod]
    public async Task ProcessRecords_InvalidNhsNumber_LogsException()
    {
        // Arrange
        var participants = new List<ParticipantsParquetMap>
            {
                new ParticipantsParquetMap { NhsNumber = 1 }
            };
        var options = new ParallelOptions();
        var screeningService = new ScreeningService { ScreeningId = "1", ScreeningName = "Test Screening" };
        const string fileName = "TestFile";

        _receiveCaasFileHelperMock.Setup(helper => helper.MapParticipant(It.IsAny<ParticipantsParquetMap>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new Participant { NhsNumber = "InvalidNHS", RecordType = Actions.New });

        // Act
        await _processCaasFile.ProcessRecords(participants, options, screeningService, fileName);

        // Assert
        _exceptionHandlerMock.Verify(handler => handler.CreateSystemExceptionLog(
            It.IsAny<Exception>(), It.IsAny<Participant>(), It.IsAny<string>()), Times.Once);
    }

    [TestMethod]
    public async Task ProcessRecords_DuplicateParticipant_LogsException()
    {
        // Arrange
        var participants = new List<ParticipantsParquetMap>
            {
                new ParticipantsParquetMap { NhsNumber = 1234567890 },
                 new ParticipantsParquetMap { NhsNumber = 1234567890 }
            };
        var options = new ParallelOptions();
        var screeningService = new ScreeningService { ScreeningId = "1", ScreeningName = "Test Screening" };
        const string fileName = "TestFile";

        _receiveCaasFileHelperMock.Setup(helper => helper.MapParticipant(It.IsAny<ParticipantsParquetMap>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new Participant { NhsNumber = "1234567890", RecordType = Actions.New });

        // Act
        await _processCaasFile.ProcessRecords(participants, options, screeningService, fileName);

        // Assert
        _exceptionHandlerMock.Verify(handler => handler.CreateSystemExceptionLog(
            It.IsAny<Exception>(), It.IsAny<Participant>(), It.IsAny<string>()), Times.AtLeastOnce);
    }

}
