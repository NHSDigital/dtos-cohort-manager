namespace NHS.CohortManager.Tests.CaasIntegrationTests;

using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
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

    [TestMethod]
    public async Task ProcessRecords_UpdateRecord_addsRecordToBatch()
    {
        // Arrange
        var addRecordMethod = _processCaasFile.GetType().GetMethod("AddRecordToBatch", BindingFlags.Instance | BindingFlags.NonPublic);
        var privateField = _processCaasFile.GetType().GetField("_currentBatch", BindingFlags.Instance | BindingFlags.NonPublic);

        var currentBatchValue = privateField.GetValue(_processCaasFile);
        if (currentBatchValue == null)
        {
            var newBatch = new Batch();
            privateField.SetValue(_processCaasFile, newBatch);
        }
        var batch = (Batch)privateField.GetValue(_processCaasFile);

        // Act
        await (Task)addRecordMethod.Invoke(_processCaasFile, new object[] { new Participant() { NhsNumber = "1234567890", RecordType = Actions.Amended }, batch, "TestFile" });


        // Assert
        Assert.AreEqual(1, batch.UpdateRecords.Count);

    }

    [TestMethod]
    public async Task AddRecordToBatch_UpdateRecord_addsRecordToBatch()
    {
        // Arrange
        Environment.SetEnvironmentVariable("DemographicURI", "DemographicURI");
        Environment.SetEnvironmentVariable("PMSUpdateParticipant", "PMSUpdateParticipant");

        _checkDemographicMock.Setup(demo => demo.PostDemographicDataAsync(It.IsAny<Participant>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        var updateParticipant = _processCaasFile.GetType().GetMethod("UpdateParticipant", BindingFlags.Instance | BindingFlags.NonPublic);

        var basicParticipantCsvRecord = new BasicParticipantCsvRecord()
        {
            FileName = "testFile",
            Participant = new BasicParticipantData() { NhsNumber = "1234567890", RecordType = Actions.Amended },
            participant = new Participant() { NhsNumber = "1234567890", RecordType = Actions.Amended }
        };

        var arguments = new object[] { basicParticipantCsvRecord, "TestName" };

        var task = (Task)updateParticipant.Invoke(_processCaasFile, arguments);

        await task;

        _checkDemographicMock.Verify(sendDemographic => sendDemographic.PostDemographicDataAsync(It.IsAny<Participant>(), It.IsAny<string>()), Times.Once);
        _callFunctionMock.Verify(sendPost => sendPost.SendPost(It.IsAny<string>(), It.IsAny<string>()), Times.Once);

        _loggerMock.Verify(x => x.Log(It.Is<LogLevel>(l => l == LogLevel.Information),
               It.IsAny<EventId>(),
               It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Called update participant")),
               It.IsAny<Exception>(),
               It.IsAny<Func<It.IsAnyType, Exception, string>>()),
           Times.Once);
    }

    [TestMethod]
    public async Task AddRecordToBatch_ValidNewRecord_AddsRecordToBatch()
    {
        // Arrange
        var method = _processCaasFile.GetType().GetMethod("AddRecordToBatch", BindingFlags.Instance | BindingFlags.NonPublic);

        var participant = new Participant() { NhsNumber = "1234567890", RecordType = Actions.New };
        var currentBatch = new Batch();
        var basicParticipantCsvRecord = new BasicParticipantCsvRecord()
        {
            Participant = new BasicParticipantData() { NhsNumber = "1234567890", RecordType = Actions.New },
            FileName = "testFile",
            participant = participant
        };

        _checkDemographicMock.Setup(m => m.PostDemographicDataAsync(It.IsAny<Participant>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        var arguments = new object[] { participant, currentBatch, "testFile" };

        // Act
        var task = (Task)method.Invoke(_processCaasFile, arguments);
        await task;

        // Assert
        Assert.AreEqual(1, currentBatch.AddRecords.Count);
        _checkDemographicMock.Verify(m => m.PostDemographicDataAsync(It.IsAny<Participant>(), It.IsAny<string>()), Times.Once);
    }

    [TestMethod]
    public async Task UpdateRecords_ValidNewRecord_ThrowsError()
    {
        // Arrange
        Environment.SetEnvironmentVariable("DemographicURI", "DemographicURI");
        Environment.SetEnvironmentVariable("PMSUpdateParticipant", "PMSUpdateParticipant");

        _checkDemographicMock.Setup(demo => demo.PostDemographicDataAsync(It.IsAny<Participant>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("some exception"));

        var updateParticipant = _processCaasFile.GetType().GetMethod("UpdateParticipant", BindingFlags.Instance | BindingFlags.NonPublic);

        var basicParticipantCsvRecord = new BasicParticipantCsvRecord()
        {
            FileName = "testFile",
            Participant = new BasicParticipantData() { NhsNumber = "1234567890", RecordType = Actions.Amended },
            participant = new Participant() { NhsNumber = "1234567890", RecordType = Actions.Amended }
        };

        var arguments = new object[] { basicParticipantCsvRecord, "TestName" };

        // Act
        var task = (Task)updateParticipant.Invoke(_processCaasFile, arguments);
        await task;

        // Assert
        _checkDemographicMock.Verify(m => m.PostDemographicDataAsync(It.IsAny<Participant>(), It.IsAny<string>()), Times.Once);
        _loggerMock.Verify(x => x.Log(It.Is<LogLevel>(l => l == LogLevel.Error),
              It.IsAny<EventId>(),
              It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Update participant function")),
              It.IsAny<Exception>(),
              It.IsAny<Func<It.IsAnyType, Exception, string>>()),
          Times.Once);


    }

    [TestMethod]
    public async Task RemoveParticipant_ValidRecord_LogsAndHandlesException()
    {
        // Arrange
        var method = _processCaasFile.GetType().GetMethod("RemoveParticipant", BindingFlags.Instance | BindingFlags.NonPublic);

        var participant = new Participant() { NhsNumber = "1234567890", RecordType = Actions.Removed };
        var basicParticipantCsvRecord = new BasicParticipantCsvRecord()
        {
            Participant = new BasicParticipantData() { NhsNumber = "1234567890", RecordType = Actions.Removed },
            FileName = "testFile",
            participant = participant
        };

        _exceptionHandlerMock.Setup(m => m.CreateDeletedRecordException(It.IsAny<BasicParticipantCsvRecord>()))
            .Returns(Task.CompletedTask);

        var arguments = new object[] { basicParticipantCsvRecord, "testFile" };

        // Act
        var task = (Task)method.Invoke(_processCaasFile, arguments);
        await task;

        // Assert
        _exceptionHandlerMock.Verify(m => m.CreateDeletedRecordException(It.IsAny<BasicParticipantCsvRecord>()), Times.Once);

        _loggerMock.Verify(x => x.Log(It.Is<LogLevel>(l => l == LogLevel.Information),
               It.IsAny<EventId>(),
               It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Logged Exception for Deleted Record")),
               It.IsAny<Exception>(),
               It.IsAny<Func<It.IsAnyType, Exception, string>>()),
           Times.Once);
    }


    [TestMethod]
    public async Task RemoveParticipant_ValidRecord_ThrowsError()
    {
        // Arrange
        var method = _processCaasFile.GetType().GetMethod("RemoveParticipant", BindingFlags.Instance | BindingFlags.NonPublic);

        var participant = new Participant() { NhsNumber = "1234567890", RecordType = Actions.Removed };
        var basicParticipantCsvRecord = new BasicParticipantCsvRecord()
        {
            Participant = new BasicParticipantData() { NhsNumber = "1234567890", RecordType = Actions.Removed },
            FileName = "testFile",
            participant = participant
        };

        _exceptionHandlerMock.Setup(m => m.CreateDeletedRecordException(It.IsAny<BasicParticipantCsvRecord>()))
            .ThrowsAsync(new Exception("some new exception"));

        var arguments = new object[] { basicParticipantCsvRecord, "testFile" };

        // Act
        var task = (Task)method.Invoke(_processCaasFile, arguments);
        await task;

        // Assert
        _exceptionHandlerMock.Verify(m => m.CreateDeletedRecordException(It.IsAny<BasicParticipantCsvRecord>()), Times.Once);

        _loggerMock.Verify(x => x.Log(It.Is<LogLevel>(l => l == LogLevel.Error),
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
        var method = _processCaasFile.GetType().GetMethod("CreateError", BindingFlags.Instance | BindingFlags.NonPublic);

        var participant = new Participant() { NhsNumber = "1234567890", RecordType = "Invalid" };

        _exceptionHandlerMock.Setup(m => m.CreateRecordValidationExceptionLog(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        var arguments = new object[] { participant, "testFile" };

        // Act
        var task = (Task)method.Invoke(_processCaasFile, arguments);
        await task;

        // Assert
        _exceptionHandlerMock.Verify(m => m.CreateRecordValidationExceptionLog(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        _loggerMock.Verify(x => x.Log(It.Is<LogLevel>(l => l == LogLevel.Error),
              It.IsAny<EventId>(),
              It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Cannot parse record type")),
              It.IsAny<Exception>(),
              It.IsAny<Func<It.IsAnyType, Exception, string>>()),
          Times.Once);
    }

    [TestMethod]
    public void ValidateDates_ShouldReturnFalse_WhenCurrentPostingEffectiveFromDateIsInvalid()
    {
        // Arrange
        var participant = new Participant
        {
            //all invalid dates
            CurrentPostingEffectiveFromDate = "123456789",
            EmailAddressEffectiveFromDate = "20230101",
            MobileNumberEffectiveFromDate = "20230101",
            UsualAddressEffectiveFromDate = "20230101",
            TelephoneNumberEffectiveFromDate = "20230101",
            PrimaryCareProviderEffectiveFromDate = "20230101"
        };

        var method = _processCaasFile.GetType().GetMethod("ValidateDates", BindingFlags.Instance | BindingFlags.NonPublic);
        var arguments = new object[] { participant };
        // Act
        var res = (bool)method.Invoke(_processCaasFile, arguments);
        // Assert
        Assert.IsFalse(res);
    }

    [TestMethod]
    public void ValidateDates_ShouldReturnTrue_WhenAllDatesAreValid()
    {
        // Arrange
        var participant = new Participant
        {
            //all valid dates
            CurrentPostingEffectiveFromDate = "20230101",
            EmailAddressEffectiveFromDate = "20230101",
            MobileNumberEffectiveFromDate = "20230101",
            UsualAddressEffectiveFromDate = "20230101",
            TelephoneNumberEffectiveFromDate = "20230101",
            PrimaryCareProviderEffectiveFromDate = "20230101"
        };

        var method = _processCaasFile.GetType().GetMethod("ValidateDates", BindingFlags.Instance | BindingFlags.NonPublic);
        var arguments = new object[] { participant };

        // Act
        var res = (bool)method.Invoke(_processCaasFile, arguments);

        // Assert
        Assert.IsTrue(res);

        _loggerMock.Verify(x => x.Log(It.Is<LogLevel>(l => l == LogLevel.Error),
             It.IsAny<EventId>(),
             It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("found in participant data")),
             It.IsAny<Exception>(),
             It.IsAny<Func<It.IsAnyType, Exception, string>>()),
         Times.Never);

    }

    [TestMethod]
    public void ValidateDates_ShouldReturnFalse_WhenEmailAddressEffectiveFromDateIsInvalid()
    {
        // Arrange
        var participant = new Participant
        {
            //Valid date formats
            CurrentPostingEffectiveFromDate = "20230101",
            EmailAddressEffectiveFromDate = "123456789",
            MobileNumberEffectiveFromDate = "20230101",
            UsualAddressEffectiveFromDate = "20230101",
            TelephoneNumberEffectiveFromDate = "20230101",
            PrimaryCareProviderEffectiveFromDate = "20230101"
        };

        var method = _processCaasFile.GetType().GetMethod("ValidateDates", BindingFlags.Instance | BindingFlags.NonPublic);
        var arguments = new object[] { participant };

        // Act
        var res = (bool)method.Invoke(_processCaasFile, arguments);

        // Assert
        Assert.IsFalse(res);
        _loggerMock.Verify(x => x.Log(It.Is<LogLevel>(l => l == LogLevel.Error),
           It.IsAny<EventId>(),
           It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("EmailAddressEffectiveFromDate")),
           It.IsAny<Exception>(),
           It.IsAny<Func<It.IsAnyType, Exception, string>>()),
       Times.Never);
    }


    [TestMethod]
    public void ValidateDates_ShouldReturnFalse_WhenMobileNumberEffectiveFromDateIsInvalid()
    {
        // Arrange
        var participant = new Participant
        {
            // Valid date format
            CurrentPostingEffectiveFromDate = "20230101", 
            EmailAddressEffectiveFromDate = "123456789", 
            MobileNumberEffectiveFromDate = "20230101", 
            UsualAddressEffectiveFromDate = "20230101", 
            TelephoneNumberEffectiveFromDate = "20230101",
            PrimaryCareProviderEffectiveFromDate = "20230101" 
        };

        var method = _processCaasFile.GetType().GetMethod("ValidateDates", BindingFlags.Instance | BindingFlags.NonPublic);
        var arguments = new object[] { participant };

        // Act
        var res = (bool)method.Invoke(_processCaasFile, arguments);

        // Assert
        Assert.IsFalse(res);
        _loggerMock.Verify(x => x.Log(It.Is<LogLevel>(l => l == LogLevel.Error),
           It.IsAny<EventId>(),
           It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("MobileNumberEffectiveFromDate")),
           It.IsAny<Exception>(),
           It.IsAny<Func<It.IsAnyType, Exception, string>>()),
       Times.Never);
    }

    [TestMethod]
    public void ValidateDates_ShouldReturnFalse_WhenUsualAddressEffectiveFromDateIsInvalid()
    {
        // Arrange
        var participant = new Participant
        {
            //all valid date formats
            CurrentPostingEffectiveFromDate = "20230101", 
            EmailAddressEffectiveFromDate = "20230101", 
            MobileNumberEffectiveFromDate = "20230101", 
            UsualAddressEffectiveFromDate = "123456789", 
            TelephoneNumberEffectiveFromDate = "20230101", 
            PrimaryCareProviderEffectiveFromDate = "20230101" 
        };

        var method = _processCaasFile.GetType().GetMethod("ValidateDates", BindingFlags.Instance | BindingFlags.NonPublic);
        var arguments = new object[] { participant };

        // Act
        var res = (bool)method.Invoke(_processCaasFile, arguments);

        // Assert
        Assert.IsFalse(res);
        _loggerMock.Verify(x => x.Log(It.Is<LogLevel>(l => l == LogLevel.Error),
           It.IsAny<EventId>(),
           It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("MobileNumberEffectiveFromDate")),
           It.IsAny<Exception>(),
           It.IsAny<Func<It.IsAnyType, Exception, string>>()),
       Times.Never);
    }


    [TestMethod]
    public void IsValidDate_ShouldReturnTrue_WhenDateIsNull()
    {
        // Arrange
        string? date = null;


        var method = _processCaasFile.GetType().GetMethod("IsValidDate", BindingFlags.Instance | BindingFlags.NonPublic);
        var arguments = new object[] { date };

        // Act
        var res = (bool)method.Invoke(_processCaasFile, arguments);

        // Assert
        Assert.IsTrue(res);
    }

    [TestMethod]
    public void IsValidDate_ShouldReturnTrue_WhenDateIsEmpty()
    {
        // Arrange
        string date = string.Empty;

        var method = _processCaasFile.GetType().GetMethod("IsValidDate", BindingFlags.Instance | BindingFlags.NonPublic);
        var arguments = new object[] { date };

        // Act


        var res = (bool)method.Invoke(_processCaasFile, arguments);

        // Assert
        Assert.IsTrue(res);
    }

    [TestMethod]
    public void IsValidDate_ShouldReturnTrue_WhenDateLengthIs8()
    {
        // Arrange
        string date = "20230101"; // Valid date format with 8 characters

        var method = _processCaasFile.GetType().GetMethod("IsValidDate", BindingFlags.Instance | BindingFlags.NonPublic);
        var arguments = new object[] { date };

        // Act
        var res = (bool)method.Invoke(_processCaasFile, arguments);

        // Assert
        Assert.IsTrue(res);
    }

    [TestMethod]
    public void IsValidDate_ShouldReturnFalse_WhenDateLengthIsGreaterThan8()
    {
        // Arrange
        string date = "20230101234"; // Invalid date with more than 8 characters

        var method = _processCaasFile.GetType().GetMethod("IsValidDate", BindingFlags.Instance | BindingFlags.NonPublic);
        var arguments = new object[] { date };

        // Act
        var res = (bool)method.Invoke(_processCaasFile, arguments);

        // Assert
        Assert.IsFalse(res);
    }

    [TestMethod]
    public void IsValidDate_ShouldReturnTrue_WhenDateLengthIsLessThan8()
    {
        // Arrange
        string date = "202301"; // Valid date with less than 8 characters

        var method = _processCaasFile.GetType().GetMethod("IsValidDate", BindingFlags.Instance | BindingFlags.NonPublic);
        var arguments = new object[] { date };

        // Act
        var res = (bool)method.Invoke(_processCaasFile, arguments);

        // Assert
        Assert.IsTrue(res);
    }


}
