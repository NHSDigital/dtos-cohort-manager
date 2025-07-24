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
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Options;

[TestClass]
public class ReceiveCaasFileTests
{
    private readonly Mock<ILogger<ReceiveCaasFile>> _mockLogger;
    private readonly Mock<IReceiveCaasFileHelper> _mockIReceiveCaasFileHelper;
    private readonly ReceiveCaasFile _receiveCaasFileInstance;
    private readonly Participant _participant;
    private readonly ParticipantsParquetMap _participantsParquetMap;
    private readonly string _blobName;
    private readonly Mock<IProcessCaasFile> _mockProcessCaasFile = new();
    private readonly Mock<IDataServiceClient<ScreeningLkp>> _mockScreeningLkpClient = new();
    private readonly Mock<IOptions<ReceiveCaasFileConfig>> _config = new();
    private readonly Mock<IExceptionHandler> _exceptionHandlerMock = new();
    private readonly Mock<IBlobStorageHelper> _blobStorageHelperMock = new();

    public ReceiveCaasFileTests()
    {
        _mockLogger = new Mock<ILogger<ReceiveCaasFile>>();
        _mockIReceiveCaasFileHelper = new Mock<IReceiveCaasFileHelper>();

        var testConfig = new ReceiveCaasFileConfig
        {
            DemographicDataServiceURL = "DemographicDataServiceURL",
            ScreeningLkpDataServiceURL = "ScreeningLkpDataServiceURL",
            DemographicURI = "DemographicURI",
            BatchSize = 2000,
            AllowDeleteRecords = true
        };

        _config.Setup(c => c.Value).Returns(testConfig);

        _receiveCaasFileInstance = new ReceiveCaasFile(_mockLogger.Object,
                                                    _mockProcessCaasFile.Object,
                                                    _mockScreeningLkpClient.Object,
                                                    _config.Object,
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

    private string GetParquetFilePath(string filename)
    {
        // Get the directory of the currently executing assembly
        string assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
        string assemblyDirectory = Path.GetDirectoryName(assemblyLocation) ?? string.Empty;

        // Try multiple potential paths
        string[] possiblePaths = new string[]
        {
        // Original path
        Path.Combine(assemblyDirectory, "../../../CaasIntegrationTests/receiveCaasFileTest", filename),

        // In the assembly directory
        Path.Combine(assemblyDirectory, filename),

        // In TestData subdirectory
        Path.Combine(assemblyDirectory, "TestData", filename),

        // One directory up in TestData
        Path.Combine(Directory.GetParent(assemblyDirectory)?.FullName ?? assemblyDirectory, "TestData", filename)
        };

        // Return the first path that exists
        foreach (string path in possiblePaths)
        {
            if (File.Exists(path))
            {
                return path;
            }
        }

        // Create a TestData directory for copying files if needed
        string testDataDirectory = Path.Combine(assemblyDirectory, "TestData");
        Directory.CreateDirectory(testDataDirectory);

        // Look for the file in parent directories
        string currentDir = assemblyDirectory;
        while (currentDir != null && Directory.GetParent(currentDir) != null)
        {
            currentDir = Directory.GetParent(currentDir).FullName;

            // Look for a TestData directory or other typical test file locations
            string[] searchDirs = new string[]
            {
            Path.Combine(currentDir, "TestData"),
            Path.Combine(currentDir, "CaasIntegrationTests/TestData"),
            Path.Combine(currentDir, "tests/TestData")
            };

            foreach (string searchDir in searchDirs)
            {
                if (Directory.Exists(searchDir))
                {
                    string sourcePath = Path.Combine(searchDir, filename);
                    if (File.Exists(sourcePath))
                    {
                        // Copy the file to the test assembly's TestData directory
                        string targetFile = Path.Combine(testDataDirectory, filename);
                        File.Copy(sourcePath, targetFile, true);
                        return targetFile;
                    }
                }
            }
        }

        // If all else fails, return the original path with a helpful message
        Console.WriteLine($"Could not find parquet file '{filename}' in any expected location.");
        Console.WriteLine("Please ensure the file exists in one of these directories:");
        foreach (string path in possiblePaths)
        {
            Console.WriteLine($" - {Path.GetDirectoryName(path)}");
        }

        return Path.Combine(assemblyDirectory, "../../../CaasIntegrationTests/receiveCaasFileTest", filename);
    }

    [TestMethod]
    public async Task Run_SuccessfulParseAndSendDataWithValidInput_SuccessfulSendToFunctionWithResponse()
    {
        // Arrange
        string parquetFilePath = GetParquetFilePath(_blobName);
        await using var fileSteam = File.OpenRead(parquetFilePath);
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

        _mockIReceiveCaasFileHelper
            .Setup(x => x.MapParticipant(_participantsParquetMap, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(_participant);

        // Act
        await _receiveCaasFileInstance.Run(fileSteam, _blobName);

        var response = MockHelpers.CreateMockHttpResponseData(HttpStatusCode.OK);

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
    public async Task Run_FileNameInvalid_CreateExceptionAndCopyFileToPoison(string blobName)
    {
        string parquetFilePath = GetParquetFilePath(_blobName); // Use _blobName as we need a valid file to open
        await using var fileSteam = File.OpenRead(parquetFilePath);
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
    [DataRow(0, "")]
    [DataRow(0, " ")]
    [DataRow(null, null)]
    public async Task Run_CannotGetScreeningId_CreateExceptionAndCopyFileToPoison(Int64 screeningId, string screeningName)
    {
        // Arrange
        string parquetFilePath = GetParquetFilePath(_blobName);
        await using var fileSteam = File.OpenRead(parquetFilePath);

        var screeningLkp = new ScreeningLkp
        {
            ScreeningName = screeningName,
            ScreeningId = screeningId,
            ScreeningWorkflowId = "TestWorkflow"
        };
        _mockScreeningLkpClient
            .Setup(x => x.GetSingleByFilter(It.IsAny<Expression<Func<ScreeningLkp, bool>>>()))
            .ReturnsAsync((ScreeningLkp)null);

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
    public async Task Run_ValidFile_ProcessAllRecords()
    {
        // Arrange
        string parquetFilePath = GetParquetFilePath(_blobName);
        await using var fileSteam = File.OpenRead(parquetFilePath);
        var tempFilePath = Path.Combine(Path.GetTempPath(), _blobName);

        var screeningLkp = new ScreeningLkp
        {
            ScreeningName = "test screening name",
            ScreeningId = 1,
            ScreeningWorkflowId = "TestWorkflow"
        };

        _mockScreeningLkpClient.Setup(x => x.GetSingleByFilter(It.IsAny<Expression<Func<ScreeningLkp, bool>>>())).ReturnsAsync(screeningLkp);
        _mockIReceiveCaasFileHelper.Setup(x => x.MapParticipant(_participantsParquetMap, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(_participant);

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
