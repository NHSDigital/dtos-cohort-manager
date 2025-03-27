namespace NHS.CohortManager.Tests.CaasIntegrationTests;

using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using Common;
using Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Model;
using Moq;
using NHS.Screening.ReceiveCaasFile;

[TestClass]
public class ProcessCaasFileTests
{
    private readonly Mock<ILogger<ProcessCaasFile>> _loggerMock = new();
    private readonly Mock<IReceiveCaasFileHelper> _receiveCaasFileHelperMock = new();
    private readonly Mock<ICreateBasicParticipantData> _createBasicParticipantDataMock = new();
    private readonly Mock<IExceptionHandler> _exceptionHandlerMock = new();
    private readonly Mock<IAddBatchToQueue> _addBatchToQueueMock = new();
    private readonly Mock<RecordsProcessedTracker> _recordsProcessedTrackerMock = new();
    private readonly Mock<DataServices.Client.IDataServiceClient<ParticipantDemographic>> _databaseClientParticipantMock = new();
    private readonly Mock<IValidateDates> _validateDates = new();
    private readonly Mock<ICallFunction> _callFunction = new();

    private readonly Mock<ICallDurableDemographicFunc> _callDurableFunc = new();
    private readonly ProcessCaasFile _processCaasFile;

    private readonly Mock<IOptions<ReceiveCaasFileConfig>> _config = new();

    // Helper method to create an instance of ProcessCaasFile with a given configuration
    private ProcessCaasFile CreateProcessCaasFile(ReceiveCaasFileConfig config)
    {
        _config.Setup(c => c.Value).Returns(config);
        return new ProcessCaasFile(
            _loggerMock.Object,
            _createBasicParticipantDataMock.Object,
            _addBatchToQueueMock.Object,
            _receiveCaasFileHelperMock.Object,
            _exceptionHandlerMock.Object,
            _databaseClientParticipantMock.Object,
            _recordsProcessedTrackerMock.Object,
            _validateDates.Object,
            _callFunction.Object,
            _callDurableFunc.Object,
            _config.Object
        );
    }

    // Default configuration helper
    private ReceiveCaasFileConfig GetDefaultConfig(bool allowDeleteRecords)
    {
        return new ReceiveCaasFileConfig
        {
            DemographicURI = "DemographicURI",
            AddQueueName = "AddQueueName",
            UpdateQueueName = "UpdateQueueName",
            AllowDeleteRecords = allowDeleteRecords,
            PMSRemoveParticipant = "PMSRemoveParticipant"
        };
    }

    [TestMethod]
    public async Task ProcessRecords_ValidParticipants_ProcessesSuccessfully()
    {
        // Arrange
        var processCaasFile = CreateProcessCaasFile(GetDefaultConfig(true));
        var participants = new List<ParticipantsParquetMap>
        {
            new ParticipantsParquetMap { NhsNumber = 1234567890 },
            new ParticipantsParquetMap { NhsNumber = 9876543210 }
        };
        var options = new ParallelOptions();
        var screeningService = new ScreeningService { ScreeningId = "1", ScreeningName = "Test Screening" };
        const string fileName = "TestFile";

        _receiveCaasFileHelperMock.Setup(helper => helper.MapParticipant(
            It.IsAny<ParticipantsParquetMap>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()))
            .ReturnsAsync(new Participant { NhsNumber = "1234567890", RecordType = Actions.New });

        _callDurableFunc.Setup(demo => demo.PostDemographicDataAsync(It.IsAny<List<ParticipantDemographic>>(), It.IsAny<string>())).ReturnsAsync(true);

        // Act
        await processCaasFile.ProcessRecords(participants, options, screeningService, fileName);

        // Assert
        _addBatchToQueueMock.Verify(queue => queue.ProcessBatch(It.IsAny<ConcurrentQueue<BasicParticipantCsvRecord>>(), It.IsAny<string>()), Times.AtLeastOnce);

        _loggerMock.Verify(x => x.Log(It.Is<LogLevel>(l => l == LogLevel.Information),
           It.IsAny<EventId>(),
           It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("sending Update Records 0 to queue")),
           It.IsAny<Exception>(),
           It.IsAny<Func<It.IsAnyType, Exception, string>>()),
       Times.Once);
    }

    [TestMethod]
    public async Task ProcessRecords_Amend_sendOneRecordToUpdateQueue()
    {
        // Arrange
        var processCaasFile = CreateProcessCaasFile(GetDefaultConfig(true));
        var participants = new List<ParticipantsParquetMap>
        {
            new ParticipantsParquetMap { NhsNumber = 1234567890 },
            new ParticipantsParquetMap { NhsNumber = 9876543210 }
        };

        var options = new ParallelOptions();
        var screeningService = new ScreeningService { ScreeningId = "1", ScreeningName = "Test Screening" };
        const string fileName = "TestFile";

        _receiveCaasFileHelperMock.Setup(helper => helper.MapParticipant(It.IsAny<ParticipantsParquetMap>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new Participant { NhsNumber = "1234567890", RecordType = Actions.Amended });

        _checkDemographicMock.Setup(demo => demo.PostDemographicDataAsync(It.IsAny<List<ParticipantDemographic>>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        // Act
        await processCaasFile.ProcessRecords(participants, options, screeningService, fileName);
        // Assert
        _addBatchToQueueMock.Verify(queue => queue.ProcessBatch(It.IsAny<ConcurrentQueue<BasicParticipantCsvRecord>>(), It.IsAny<string>()), Times.AtLeastOnce);

        _addBatchToQueueMock.Verify(queue => queue.ProcessBatch(It.IsAny<ConcurrentQueue<BasicParticipantCsvRecord>>(), It.IsAny<string>()), Times.AtLeastOnce);
        _loggerMock.Verify(x => x.Log(It.Is<LogLevel>(l => l == LogLevel.Information),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("sending 0 records to Add queue")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [TestMethod]
    public async Task ProcessRecords_InvalidNhsNumber_LogsException()
    {
        // Arrange
        var processCaasFile = CreateProcessCaasFile(GetDefaultConfig(true));
        var participants = new List<ParticipantsParquetMap>
        {
            new ParticipantsParquetMap { NhsNumber = 1 }
        };
        var options = new ParallelOptions();
        var screeningService = new ScreeningService { ScreeningId = "1", ScreeningName = "Test Screening" };
        const string fileName = "TestFile";

        _receiveCaasFileHelperMock.Setup(helper => helper.MapParticipant(
            It.IsAny<ParticipantsParquetMap>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()))
            .ReturnsAsync(new Participant { NhsNumber = "InvalidNHS", RecordType = Actions.New });

        // Act
        await processCaasFile.ProcessRecords(participants, options, screeningService, fileName);

        // Assert
        _exceptionHandlerMock.Verify(handler => handler.CreateSystemExceptionLog(
            It.IsAny<Exception>(), It.IsAny<Participant>(), It.IsAny<string>()),
            Times.Once);
    }

    [TestMethod]
    public async Task ProcessRecords_DuplicateParticipant_LogsException()
    {
        // Arrange
        var processCaasFile = CreateProcessCaasFile(GetDefaultConfig(true));
        var participants = new List<ParticipantsParquetMap>
        {
            new ParticipantsParquetMap { NhsNumber = 1234567890 },
            new ParticipantsParquetMap { NhsNumber = 1234567890 }
        };
        var options = new ParallelOptions();
        var screeningService = new ScreeningService { ScreeningId = "1", ScreeningName = "Test Screening" };
        const string fileName = "TestFile";

        _receiveCaasFileHelperMock.Setup(helper => helper.MapParticipant(
            It.IsAny<ParticipantsParquetMap>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()))
            .ReturnsAsync(new Participant { NhsNumber = "1234567890", RecordType = Actions.New });

        // Act
        await processCaasFile.ProcessRecords(participants, options, screeningService, fileName);

        // Assert
        _exceptionHandlerMock.Verify(handler => handler.CreateSystemExceptionLog(
            It.IsAny<Exception>(), It.IsAny<Participant>(), It.IsAny<string>()),
            Times.AtLeastOnce);
    }

    [TestMethod]
    public async Task AddRecordToBatch_UpdateRecord_addsRecordToBatch()
    {
        // Arrange
        var processCaasFile = CreateProcessCaasFile(GetDefaultConfig(true));
        _callDurableFunc.Setup(demo => demo.PostDemographicDataAsync(It.IsAny<List<ParticipantDemographic>>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        var updateParticipant = processCaasFile.GetType().GetMethod("DeleteOldDemographicRecord", BindingFlags.Instance | BindingFlags.NonPublic);
        var basicParticipantCsvRecord = new BasicParticipantCsvRecord()
        {
            FileName = "testFile",
            Participant = new BasicParticipantData { NhsNumber = "1234567890", RecordType = Actions.Amended },
            participant = new Participant { NhsNumber = "1234567890", RecordType = Actions.Amended }
        };
        var arguments = new object[] { basicParticipantCsvRecord, "TestName" };

        // Act
        var task = (Task)updateParticipant.Invoke(processCaasFile, arguments);
        await task;

        _callDurableFunc.Verify(sendDemographic => sendDemographic.PostDemographicDataAsync(It.IsAny<List<ParticipantDemographic>>(), It.IsAny<string>()), Times.Never);

        _loggerMock.Verify(x => x.Log(It.Is<LogLevel>(l => l == LogLevel.Warning),
               It.IsAny<EventId>(),
               It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("The participant could not be found")),
               It.IsAny<Exception>(),
               It.IsAny<Func<It.IsAnyType, Exception, string>>()),
           Times.Once);
    }

    [TestMethod]
    public async Task AddRecordToBatch_ValidNewRecord_AddsRecordToBatch()
    {
        // Arrange
        var processCaasFile = CreateProcessCaasFile(GetDefaultConfig(true));
        var method = processCaasFile.GetType().GetMethod("AddRecordToBatch", BindingFlags.Instance | BindingFlags.NonPublic);
        var participant = new Participant { NhsNumber = "1234567890", RecordType = Actions.New };
        var currentBatch = new Batch();

        _callDurableFunc.Setup(m => m.PostDemographicDataAsync(It.IsAny<List<ParticipantDemographic>>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        var arguments = new object[] { participant, currentBatch, "testFile" };

        // Act
        var task = (Task)method.Invoke(processCaasFile, arguments);
        await task;

        // Assert
        Assert.AreEqual(1, currentBatch.AddRecords.Count);
    }

    [TestMethod]
    public async Task UpdateRecords_ValidNewRecord_ThrowsError()
    {
        // Arrange
        var processCaasFile = CreateProcessCaasFile(GetDefaultConfig(true));
        var basicParticipantCsvRecord = new BasicParticipantCsvRecord()
        {
            FileName = "testFile",
            Participant = new BasicParticipantData { NhsNumber = "1234567890", RecordType = Actions.Amended },
            participant = new Participant { NhsNumber = "1234567890", RecordType = Actions.Amended }
        };

        var response = new ParticipantDemographic { ParticipantId = 1, GivenName = "" };
        _databaseClientParticipantMock.Setup(x => x.GetSingleByFilter(
            It.IsAny<Expression<Func<ParticipantDemographic, bool>>>()))
            .Returns(Task.FromResult(response));

        _databaseClientParticipantMock.Setup(x => x.GetSingleByFilter(It.IsAny<Expression<Func<ParticipantDemographic, bool>>>())).Returns(Task.FromResult(response));

        _callDurableFunc.Setup(demo => demo.PostDemographicDataAsync(It.IsAny<List<ParticipantDemographic>>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("some exception"));

        var updateParticipant = processCaasFile.GetType().GetMethod("DeleteOldDemographicRecord", BindingFlags.Instance | BindingFlags.NonPublic);
        var arguments = new object[] { basicParticipantCsvRecord, "TestName" };

        // Act
        var task = (Task)updateParticipant.Invoke(processCaasFile, arguments);
        await task;

        // Assert
        _loggerMock.Verify(x => x.Log(
            It.Is<LogLevel>(l => l == LogLevel.Information),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Deleting old Demographic record was not successful")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
    }

    [TestMethod]
    public async Task RemoveParticipant_ValidRecordNotAllowDeleteRecords_LogsAndHandlesException()
    {
        // Arrange
        var processCaasFile = CreateProcessCaasFile(GetDefaultConfig(false));
        var method = processCaasFile.GetType().GetMethod("RemoveParticipant", BindingFlags.Instance | BindingFlags.NonPublic);
        var participant = new Participant { NhsNumber = "1234567890", RecordType = Actions.Removed };
        var basicParticipantCsvRecord = new BasicParticipantCsvRecord
        {
            Participant = new BasicParticipantData { NhsNumber = "1234567890", RecordType = Actions.Removed },
            FileName = "testFile",
            participant = participant
        };

        _exceptionHandlerMock.Setup(m => m.CreateDeletedRecordException(
            It.IsAny<BasicParticipantCsvRecord>()))
            .Returns(Task.CompletedTask);

        var arguments = new object[] { basicParticipantCsvRecord, "testFile" };

        // Act
        var task = (Task)method.Invoke(processCaasFile, arguments);
        await task;

        // Assert: expect CreateDeletedRecordException to be invoked
        _exceptionHandlerMock.Verify(m => m.CreateDeletedRecordException(
            It.IsAny<BasicParticipantCsvRecord>()), Times.Once);
        _callFunction.Verify(x => x.SendPost(
            It.Is<string>(s => s.Contains("PMSRemoveParticipant")), It.IsAny<string>()),
            Times.Never);
        _loggerMock.Verify(x => x.Log(
            It.Is<LogLevel>(l => l == LogLevel.Information),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("AllowDeleteRecords flag is false, exception raised for delete record.")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [TestMethod]
    public async Task RemoveParticipant_ValidRecordAllowDeleteRecords_LogsInformation()
    {
        // Arrange
        var processCaasFile = CreateProcessCaasFile(GetDefaultConfig(true));
        var method = processCaasFile.GetType().GetMethod("RemoveParticipant", BindingFlags.Instance | BindingFlags.NonPublic);
        var participant = new Participant { NhsNumber = "1234567890", RecordType = Actions.Removed };
        var basicParticipantCsvRecord = new BasicParticipantCsvRecord
        {
            Participant = new BasicParticipantData { NhsNumber = "1234567890", RecordType = Actions.Removed },
            FileName = "testFile",
            participant = participant
        };

        var arguments = new object[] { basicParticipantCsvRecord, "testFile" };

        // Act
        var task = (Task)method.Invoke(processCaasFile, arguments);
        await task;

        // Assert: expect call to SendPost to occur
        _callFunction.Verify(x => x.SendPost(
            It.Is<string>(s => s.Contains("PMSRemoveParticipant")), It.IsAny<string>()),
            Times.Once);
        _loggerMock.Verify(x => x.Log(
            It.Is<LogLevel>(l => l == LogLevel.Information),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("AllowDeleteRecords flag is true")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [TestMethod]
    public async Task RemoveParticipant_ValidRecord_ThrowsError()
    {
        // Arrange
        var processCaasFile = CreateProcessCaasFile(GetDefaultConfig(false));
        var method = processCaasFile.GetType().GetMethod("RemoveParticipant", BindingFlags.Instance | BindingFlags.NonPublic);
        var participant = new Participant { NhsNumber = "1234567890", RecordType = Actions.Removed };
        var basicParticipantCsvRecord = new BasicParticipantCsvRecord
        {
            Participant = new BasicParticipantData { NhsNumber = "1234567890", RecordType = Actions.Removed },
            FileName = "testFile",
            participant = participant
        };

        _exceptionHandlerMock.Setup(m => m.CreateDeletedRecordException(
            It.IsAny<BasicParticipantCsvRecord>()))
            .ThrowsAsync(new Exception("some new exception"));

        var arguments = new object[] { basicParticipantCsvRecord, "testFile" };

        // Act
        var task = (Task)method.Invoke(processCaasFile, arguments);
        await task;

        // Assert: expect CreateDeletedRecordException to be invoked and error logged
        _exceptionHandlerMock.Verify(m => m.CreateDeletedRecordException(
            It.IsAny<BasicParticipantCsvRecord>()), Times.Once);
        _loggerMock.Verify(x => x.Log(
            It.Is<LogLevel>(l => l == LogLevel.Error),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Remove participant function failed")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [TestMethod]
    public async Task CreateError_ExceptionOccurred_LogsAndHandlesException()
    {
        // Arrange
        var processCaasFile = CreateProcessCaasFile(GetDefaultConfig(true));
        var method = processCaasFile.GetType().GetMethod("CreateError", BindingFlags.Instance | BindingFlags.NonPublic);
        var participant = new Participant { NhsNumber = "1234567890", RecordType = "Invalid" };

        _exceptionHandlerMock.Setup(m => m.CreateRecordValidationExceptionLog(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        var arguments = new object[] { participant, "testFile" };

        // Act
        var task = (Task)method.Invoke(processCaasFile, arguments);
        await task;

        // Assert
        _exceptionHandlerMock.Verify(m => m.CreateRecordValidationExceptionLog(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Once);
        _loggerMock.Verify(x => x.Log(
            It.Is<LogLevel>(l => l == LogLevel.Error),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Cannot parse record type")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }
}
