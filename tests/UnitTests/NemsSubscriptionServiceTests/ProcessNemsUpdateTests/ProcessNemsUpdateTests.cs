namespace NHS.CohortManager.Tests.UnitTests.ProcessNemsUpdateTests;

using NHS.Screening.ProcessNemsUpdate;
using Moq;
using Microsoft.Extensions.Logging;
using Common.Interfaces;
using Common;
using Microsoft.Extensions.Options;
using Model;
using System.Net;
using System.Text.Json;
using System.Collections.Concurrent;

[TestClass]
public class ProcessNemsUpdateTests
{
    private readonly Mock<ILogger<ProcessNemsUpdate>> _loggerMock = new();
    private static readonly Mock<IFhirPatientDemographicMapper> _fhirPatientDemographicMapperMock = new();
    private readonly Mock<ICreateBasicParticipantData> _createBasicParticipantDataMock = new();
    private readonly Mock<IAddBatchToQueue> _addBatchToQueueMock = new();
    private readonly Mock<IHttpClientFunction> _httpClientFunctionMock = new();
    private readonly Mock<IOptions<ProcessNemsUpdateConfig>> _config = new();
    private readonly ProcessNemsUpdate _sut;
    const string _validNhsNumber = "9000000009";
    const string _fileName = "fileName";

    public ProcessNemsUpdateTests()
    {
        var testConfig = new ProcessNemsUpdateConfig
        {
            RetrievePdsDemographicURL = "RetrievePdsDemographic",
            NemsMessages = "nems-messages",
            UpdateQueueName = "update-participant-queue"
        };

        _config.Setup(c => c.Value).Returns(testConfig);

        _sut = new ProcessNemsUpdate(
            _loggerMock.Object,
            _fhirPatientDemographicMapperMock.Object,
            _createBasicParticipantDataMock.Object,
            _addBatchToQueueMock.Object,
            _httpClientFunctionMock.Object,
            _config.Object
        );

        _httpClientFunctionMock.Reset();
        _fhirPatientDemographicMapperMock.Reset();

        _fhirPatientDemographicMapperMock.Setup(x => x.ParseFhirJsonNhsNumber(It.IsAny<string>())).Returns(_validNhsNumber);

        _httpClientFunctionMock.Setup(x => x.SendGetResponse("RetrievePdsDemographic", It.IsAny<Dictionary<string, string>>()))
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK });
    }

    [TestMethod]
    public async Task Run_FailsToRetrieveNhsNumberFromNemsUpdateFile_LogsError()
    {
        // Arrange
        string fhirJson = LoadTestJson("mock-patient");
        await using var fileStream = File.OpenRead(fhirJson);

        _fhirPatientDemographicMapperMock.Setup(x => x.ParseFhirJsonNhsNumber(It.IsAny<string>())).Throws(new FormatException("error"));

        // Act
        await _sut.Run(fileStream, _fileName);

        // Assert
        _fhirPatientDemographicMapperMock.Verify(x => x.ParseFhirJsonNhsNumber(It.IsAny<string>()), Times.Once);

        _httpClientFunctionMock.Verify(x => x.SendGetResponse("RetrievePdsDemographic", It.IsAny<Dictionary<string, string>>()), Times.Never);

        _loggerMock.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("There was an error getting the NHS number from the file.")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        Times.Once);
    }

    [TestMethod]
    public async Task Run_FailsToRetrievePdsRecord_LogsError()
    {
        // Arrange
        string fhirJson = LoadTestJson("mock-patient");
        await using var fileStream = File.OpenRead(fhirJson);

        _httpClientFunctionMock.Setup(x => x.SendGetResponse("RetrievePdsDemographic", It.IsAny<Dictionary<string, string>>())).Throws(new Exception("error"));

        // Act
        await _sut.Run(fileStream, _fileName);

        // Assert
        _fhirPatientDemographicMapperMock.Verify(x => x.ParseFhirJsonNhsNumber(It.IsAny<string>()), Times.Once);

        _httpClientFunctionMock.Verify(x => x.SendGetResponse("RetrievePdsDemographic", It.IsAny<Dictionary<string, string>>()), Times.Once);

        _loggerMock.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("There was an error retrieving the PDS record.")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        Times.Once);
    }

    [TestMethod]
    public async Task Run_RetrievePdsRecordReturnsNonSuccessStatus_LogsError()
    {
        // Arrange
        string fhirJson = LoadTestJson("mock-patient");
        await using var fileStream = File.OpenRead(fhirJson);

        _httpClientFunctionMock.Setup(x => x.SendGetResponse("RetrievePdsDemographic", It.IsAny<Dictionary<string, string>>()))
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.NotFound });

        // Act
        await _sut.Run(fileStream, _fileName);

        // Assert
        _fhirPatientDemographicMapperMock.Verify(x => x.ParseFhirJsonNhsNumber(It.IsAny<string>()), Times.Once);

        _httpClientFunctionMock.Verify(x => x.SendGetResponse("RetrievePdsDemographic", It.IsAny<Dictionary<string, string>>()), Times.Once);

        _loggerMock.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("The PDS response was not successful. StatusCode: NotFound")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        Times.Once);

        _loggerMock.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("There is no PDS record, unable to continue.")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        Times.Once);
    }

    [TestMethod]
    public async Task Run_NhsNumberFromNemsUpdateFileDoesNotMatchRetrievedPdsRecordNhsNumber_LogsError()
    {
        // Arrange
        string fhirJson = LoadTestJson("mock-patient");
        await using var fileStream = File.OpenRead(fhirJson);

        _httpClientFunctionMock.Setup(x => x.GetResponseText(It.IsAny<HttpResponseMessage>()))
            .ReturnsAsync(JsonSerializer.Serialize(new PdsDemographic() { NhsNumber = "123" }));

        // Act
        await _sut.Run(fileStream, _fileName);

        // Assert
        _fhirPatientDemographicMapperMock.Verify(x => x.ParseFhirJsonNhsNumber(It.IsAny<string>()), Times.Once);

        _httpClientFunctionMock.Verify(x => x.SendGetResponse("RetrievePdsDemographic", It.IsAny<Dictionary<string, string>>()), Times.Once);

        _loggerMock.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("NHS numbers do not match.")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        Times.Once);
    }

    [TestMethod]
    public async Task Run_NemsUpdateMatchesPdsRecord_LogsInformation()
    {
        // Arrange
        string fhirJson = LoadTestJson("mock-patient");
        await using var fileStream = File.OpenRead(fhirJson);

        _httpClientFunctionMock.Setup(x => x.GetResponseText(It.IsAny<HttpResponseMessage>()))
            .ReturnsAsync(JsonSerializer.Serialize(new PdsDemographic() { NhsNumber = _validNhsNumber }));

        // Act
        await _sut.Run(fileStream, _fileName);

        // Assert
        _fhirPatientDemographicMapperMock.Verify(x => x.ParseFhirJsonNhsNumber(It.IsAny<string>()), Times.Once);

        _httpClientFunctionMock.Verify(x => x.SendGetResponse("RetrievePdsDemographic", It.IsAny<Dictionary<string, string>>()), Times.Once);

        _loggerMock.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("NHS numbers match.")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        Times.Once);

        _addBatchToQueueMock.Verify(queue => queue.ProcessBatch(It.IsAny<ConcurrentQueue<BasicParticipantCsvRecord>>(), It.IsAny<string>()), Times.AtLeastOnce);
    }

    private static string LoadTestJson(string filename)
    {
        // Add .json extension if not already present
        string filenameWithExtension = filename.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
            ? filename
            : $"{filename}.json";

        // Get the directory of the currently executing assembly
        string assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
        string assemblyDirectory = Path.GetDirectoryName(assemblyLocation) ?? string.Empty;

        // Try the original path first
        string originalPath = Path.Combine(assemblyDirectory, "../../../PatientMocks", filenameWithExtension);
        if (File.Exists(originalPath))
        {
            return originalPath;
        }

        // Try the alternative path
        string alternativePath = Path.Combine(assemblyDirectory, "../../../NemsSubscriptionServiceTests/ProcessNemsUpdateTests/PatientMocks", filenameWithExtension);
        if (File.Exists(alternativePath))
        {
            return alternativePath;
        }

        return string.Empty;
    }
}
