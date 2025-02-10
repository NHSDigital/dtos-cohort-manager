namespace NHS.CohortManager.Tests.CaasIntegrationTests;

using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Common;
using Common.Interfaces;
using NHS.Screening.ReceiveCaasFile;
using Data.Database;
using Model;
using DataServices.Client;
using NHS.CohortManager.Tests.TestUtils;

[TestClass]
public class ReceiveCaasFileTests
{
    private readonly Mock<ILogger<ReceiveCaasFile>> _mockLogger;
    private readonly Mock<ICallFunction> _mockICallFunction;
    private readonly Mock<IReceiveCaasFileHelper> _mockIReceiveCaasFileHelper;
    private readonly ReceiveCaasFile _receiveCaasFileInstance;
    private readonly Participant _participant;
    private readonly ParticipantsParquetMap _participantsParquetMap;
    private readonly string _blobName;
    private readonly Mock<IProcessCaasFile> _mockProcessCaasFile = new();

    public ReceiveCaasFileTests()
    {
        _mockLogger = new Mock<ILogger<ReceiveCaasFile>>();
        _mockICallFunction = new Mock<ICallFunction>();
        _mockIReceiveCaasFileHelper = new Mock<IReceiveCaasFileHelper>();

        Environment.SetEnvironmentVariable("BatchSize", "2000");

        _receiveCaasFileInstance = new ReceiveCaasFile(_mockLogger.Object, _mockIReceiveCaasFileHelper.Object, _mockProcessCaasFile.Object);
        _blobName = "add_1_-_CAAS_BREAST_SCREENING_COHORT.parquet";

        _participant = new Participant()
        {
            FirstName = "John",
            FamilyName = "Regans",
            NhsNumber = "1111122202",
            RecordType = Actions.New
        };
        _participantsParquetMap = new ParticipantsParquetMap()
        {
            FirstName = "John",
            SurnamePrefix = "Regans",
            NhsNumber = 1111122202,
            RecordType = Actions.New
        };
    }

    [TestMethod]
    public async Task Run_SuccessfulParseAndSendDataWithValidInput_SuccessfulSendToFunctionWithResponse()
    {
        // Arrange

        await using var fileSteam = File.OpenRead(_blobName);
        var tempFilePath = Path.Combine(Path.GetTempPath(), _blobName);
        var screeningService = new ScreeningService
        {
            ScreeningName = "test screening name",
            ScreeningId = "1",
            ScreeningWorkflowId = "TestWorkflow"
        };
        _mockIReceiveCaasFileHelper.Setup(x => x.CheckFileName(It.IsAny<string>(), It.IsAny<FileNameParser>(), It.IsAny<string>()))
            .Returns(Task.FromResult(true));
        _mockIReceiveCaasFileHelper.Setup(x => x.GetScreeningService(It.IsAny<FileNameParser>())).Returns(Task.FromResult<ScreeningService?>(screeningService));

        _mockICallFunction.Setup(callFunction => callFunction.SendPost(It.IsAny<string>(), It.IsAny<string>())).Verifiable();


        _mockIReceiveCaasFileHelper.Setup(x => x.MapParticipant(_participantsParquetMap, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult<Participant?>(_participant));
        // Act
        await _receiveCaasFileInstance.Run(fileSteam, _blobName);

        var response = MockHelpers.CreateMockHttpResponseData(HttpStatusCode.OK);
        _mockICallFunction.Setup(call => call.SendPost(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult(response));

        _mockLogger.Verify(x => x.Log(It.Is<LogLevel>(l => l == LogLevel.Information),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"All rows processed for file named {_blobName}.")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);

        Assert.IsFalse(File.Exists(tempFilePath), "Temporary file was not deleted.");
    }

    [TestMethod]
    [DataRow("F9B292BSS_20241201121212_n1.parquet")]
    public async Task Run_FileNameIsIncorrect_LogFileNameIsInvalid(string blobName)
    {

        await using var fileSteam = File.OpenRead(_blobName);
        var tempFilePath = Path.Combine(Path.GetTempPath(), _blobName);

        // Act
        await _receiveCaasFileInstance.Run(fileSteam, blobName);

        _mockLogger.Verify(x => x.Log(It.Is<LogLevel>(l => l == LogLevel.Error),
                 It.IsAny<EventId>(),
                 It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("File name is invalid.")),
                 It.IsAny<Exception>(),
                 It.IsAny<Func<It.IsAnyType, Exception, string>>()),
             Times.Once);

        Assert.IsFalse(File.Exists(tempFilePath), "Temporary file was not deleted.");

    }

    [TestMethod]
    public async Task Run_fileNameChecksTrowsError_ErrorIsThrown()
    {

        await using var fileSteam = File.OpenRead(_blobName);
        var tempFilePath = Path.Combine(Path.GetTempPath(), _blobName);

        _mockIReceiveCaasFileHelper.Setup(x => x.CheckFileName(_blobName, It.IsAny<FileNameParser>(), It.IsAny<string>()))
        .Throws(new Exception("There was a problem checking file name"));
        // Act
        await _receiveCaasFileInstance.Run(fileSteam, _blobName);

        _mockLogger.Verify(x => x.Log(It.Is<LogLevel>(l => l == LogLevel.Error),
                 It.IsAny<EventId>(),
                 It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Stack Trace:")),
                 It.IsAny<Exception>(),
                 It.IsAny<Func<It.IsAnyType, Exception, string>>()),
             Times.Once);

        Assert.IsFalse(File.Exists(tempFilePath), "Temporary file was not deleted.");
    }

    [TestMethod]
    [DataRow("", "")]
    [DataRow(" ", " ")]
    [DataRow(null, null)]
    public async Task Run_cannotGetScreeningId_LogsError(string screeningId, string screeningName)
    {
        // Arrange

        await using var fileSteam = File.OpenRead(_blobName);

        _mockIReceiveCaasFileHelper.Setup(x => x.CheckFileName(_blobName, It.IsAny<FileNameParser>(), It.IsAny<string>()))
        .Returns(Task.FromResult(true));

        var screeningService = new ScreeningService();
        screeningService.ScreeningId = screeningId;
        screeningService.ScreeningName = screeningName;

        _mockIReceiveCaasFileHelper.Setup(x => x.GetScreeningService(It.IsAny<FileNameParser>())).Returns(Task.FromResult<ScreeningService?>(screeningService));

        _mockICallFunction.Setup(callFunction => callFunction.SendPost(It.IsAny<string>(), It.IsAny<string>())).Verifiable();

        // Act
        await _receiveCaasFileInstance.Run(fileSteam, _blobName);

        _mockLogger.Verify(x => x.Log(It.Is<LogLevel>(l => l == LogLevel.Error),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("The Screening id or screening name was null or empty")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()),
        Times.Once);
    }

    [TestMethod]
    public async Task Run_ProcessesChunksInParallel()
    {
        // Arrange
        await using var fileSteam = File.OpenRead(_blobName);
        var tempFilePath = Path.Combine(Path.GetTempPath(), _blobName);
        _mockIReceiveCaasFileHelper.Setup(x => x.CheckFileName(It.IsAny<string>(), It.IsAny<FileNameParser>(), It.IsAny<string>()))
            .Returns(Task.FromResult(true));

        _mockICallFunction.Setup(callFunction => callFunction.SendPost(It.IsAny<string>(), It.IsAny<string>())).Verifiable();

        var screeningService = new ScreeningService
        {
            ScreeningName = "test screening name",
            ScreeningId = "1",
            ScreeningWorkflowId = "TestWorkflow"
        };
        _mockIReceiveCaasFileHelper.Setup(x => x.GetScreeningService(It.IsAny<FileNameParser>())).Returns(Task.FromResult<ScreeningService?>(screeningService));

        _mockIReceiveCaasFileHelper.Setup(x => x.MapParticipant(_participantsParquetMap, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult<Participant?>(_participant));

        var batchSize = 1;
        Environment.SetEnvironmentVariable("BatchSize", batchSize.ToString());

        // Act
        await _receiveCaasFileInstance.Run(fileSteam, _blobName);

        // Assert
        _mockProcessCaasFile.Verify(x => x.ProcessRecords(It.Is<List<ParticipantsParquetMap>>(list => list.Count == batchSize), It.IsAny<ParallelOptions>(), It.IsAny<ScreeningService>(), _blobName), Times.Exactly(1));

        _mockLogger.Verify(x => x.Log(It.Is<LogLevel>(l => l == LogLevel.Information),
               It.IsAny<EventId>(),
               It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"All rows processed for file named {_blobName}.")),
               It.IsAny<Exception>(),
               It.IsAny<Func<It.IsAnyType, Exception, string>>()),
           Times.Once);

        Assert.IsFalse(File.Exists(tempFilePath), "Temporary file was not deleted.");
    }
}
