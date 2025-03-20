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
using System.Linq.Expressions;

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
    private readonly Mock<IDataServiceClient<ScreeningLkp>> _mockScreeningLkpClient = new();
    private readonly Mock<IExceptionHandler> _exceptionHandlerMock = new();
    private readonly Mock<IBlobStorageHelper> _blobStorageHelperMock = new();

    public ReceiveCaasFileTests()
    {
        _mockLogger = new Mock<ILogger<ReceiveCaasFile>>();
        _mockICallFunction = new Mock<ICallFunction>();
        _mockIReceiveCaasFileHelper = new Mock<IReceiveCaasFileHelper>();

        Environment.SetEnvironmentVariable("BatchSize", "2000");

        _receiveCaasFileInstance = new ReceiveCaasFile(_mockLogger.Object,
                                                    _mockProcessCaasFile.Object,
                                                    _mockScreeningLkpClient.Object,
                                                    _blobStorageHelperMock.Object,   

                                                    _exceptionHandlerMock.Object);
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
        var screeningLkp = new ScreeningLkp
        {
            ScreeningName = "test screening name",
            ScreeningId = 1,
            ScreeningWorkflowId = "TestWorkflow"
        };
        _mockScreeningLkpClient
            .Setup(x => x.GetSingleByFilter(It.IsAny<Expression<Func<ScreeningLkp, bool>>>()))
            .ReturnsAsync(screeningLkp);
        _mockICallFunction
            .Setup(callFunction => callFunction.SendPost(It.IsAny<string>(), It.IsAny<string>()))
            .Verifiable();
        _mockIReceiveCaasFileHelper
            .Setup(x => x.MapParticipant(_participantsParquetMap, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.FromResult<Participant?>(_participant));
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
    public async Task Run_FileNameIsIncorrect_CreateExceptionAndCopyBlobToPoison(string blobName)
    {
        // Arrange
        await using var fileSteam = File.OpenRead(_blobName);
        var tempFilePath = Path.Combine(Path.GetTempPath(), _blobName);

        // Act
        await _receiveCaasFileInstance.Run(fileSteam, blobName);

        // Assert
        _exceptionHandlerMock
            .Verify(x => x.CreateSystemExceptionLogFromNhsNumber(
                It.IsAny<ArgumentException>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()
            ));

        _blobStorageHelperMock
            .Verify(x => x.CopyFileToPoisonAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()
            ));

        Assert.IsFalse(File.Exists(tempFilePath), "Temporary file was not deleted.");

    }

    [TestMethod]
    public async Task Run_fileNameChecksThrowsError_CreateExceptionAndCopyBlobToPoison()
    {
        // Arrange
        await using var fileSteam = File.OpenRead(_blobName);
        var tempFilePath = Path.Combine(Path.GetTempPath(), _blobName);

        // Act
        await _receiveCaasFileInstance.Run(fileSteam, _blobName);

        // Assert
        _exceptionHandlerMock
            .Verify(x => x.CreateSystemExceptionLogFromNhsNumber(
                It.IsAny<Exception>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()
            ));

        _blobStorageHelperMock
            .Verify(x => x.CopyFileToPoisonAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()
            ));
    }

    [TestMethod]
    [DataRow(0, "")]
    [DataRow(0, " ")]
    [DataRow(null, null)]
    public async Task Run_CannotGetScreeningId_CreateExceptionAndCopyBlobToPoison(Int64 screeningId, string screeningName)
    {
        // Arrange
        await using var fileSteam = File.OpenRead(_blobName);

        var screeningLkp = new ScreeningLkp
        {
            ScreeningName = screeningName,
            ScreeningId = screeningId,
            ScreeningWorkflowId = "TestWorkflow"
        };
        _mockScreeningLkpClient
            .Setup(x => x.GetSingleByFilter(It.IsAny<Expression<Func<ScreeningLkp, bool>>>()))
            .ReturnsAsync((ScreeningLkp)null);

        _mockICallFunction.Setup(callFunction => callFunction.SendPost(It.IsAny<string>(), It.IsAny<string>())).Verifiable();

        // Act
        await _receiveCaasFileInstance.Run(fileSteam, _blobName);

        // Assert
        _exceptionHandlerMock
            .Verify(x => x.CreateSystemExceptionLogFromNhsNumber(
                It.IsAny<ArgumentException>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()
            ));

        _blobStorageHelperMock
            .Verify(x => x.CopyFileToPoisonAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()
            ));
    }

    [TestMethod]
    public async Task Run_ProcessesChunksInParallel()
    {
        // Arrange
        await using var fileSteam = File.OpenRead(_blobName);
        var tempFilePath = Path.Combine(Path.GetTempPath(), _blobName);


        _mockICallFunction.Setup(callFunction => callFunction.SendPost(It.IsAny<string>(), It.IsAny<string>())).Verifiable();

        var screeningLkp = new ScreeningLkp
        {
            ScreeningName = "test screening name",
            ScreeningId = 1,
            ScreeningWorkflowId = "TestWorkflow"
        };

        _mockScreeningLkpClient.Setup(x => x.GetSingleByFilter(It.IsAny<Expression<Func<ScreeningLkp, bool>>>())).ReturnsAsync(screeningLkp);
        _mockIReceiveCaasFileHelper.Setup(x => x.MapParticipant(_participantsParquetMap, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult<Participant?>(_participant));

        var batchSize = 1;
        Environment.SetEnvironmentVariable("BatchSize", batchSize.ToString());

        // Act
        await _receiveCaasFileInstance.Run(fileSteam, _blobName);

        // Assert
        _mockProcessCaasFile
            .Verify(x => x.ProcessRecords(
                It.Is<List<ParticipantsParquetMap>>(list => list.Count == batchSize),
                It.IsAny<ParallelOptions>(),
                It.IsAny<ScreeningLkp>(),
                _blobName),
            Times.Exactly(1));

        _mockLogger.Verify(x => x.Log(It.Is<LogLevel>(l => l == LogLevel.Information),
               It.IsAny<EventId>(),
               It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"All rows processed for file named {_blobName}.")),
               It.IsAny<Exception>(),
               It.IsAny<Func<It.IsAnyType, Exception, string>>()),
           Times.Once);

        Assert.IsFalse(File.Exists(tempFilePath), "Temporary file was not deleted.");
    }
}
