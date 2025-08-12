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
using System.Text;
using DataServices.Client;
using System.Linq.Expressions;

[TestClass]
public class ProcessNemsUpdateTests
{
    private readonly Mock<ILogger<ProcessNemsUpdate>> _loggerMock = new();
    private static readonly Mock<IFhirPatientDemographicMapper> _fhirPatientDemographicMapperMock = new();
    private readonly Mock<ICreateBasicParticipantData> _createBasicParticipantDataMock = new();
    private readonly Mock<IAddBatchToQueue> _addBatchToQueueMock = new();
    private readonly Mock<IHttpClientFunction> _httpClientFunctionMock = new();
    private readonly Mock<IOptions<ProcessNemsUpdateConfig>> _config = new();
    private readonly Mock<IExceptionHandler> _exceptionHandlerMock = new();
    private readonly Mock<IDataServiceClient<ParticipantDemographic>> _participantDemographicMock = new();
    private readonly Mock<IBlobStorageHelper> _blobStorageHelperMock = new();
    private readonly ProcessNemsUpdate _sut;
    const string _validNhsNumber = "9000000009";
    const string _fileName = "fileName";

    public ProcessNemsUpdateTests()
    {
        var testConfig = new ProcessNemsUpdateConfig
        {
            RetrievePdsDemographicURL = "RetrievePdsDemographic",
            NemsMessages = "nems-updates",
            UnsubscribeNemsSubscriptionUrl = "Unsubscribe",
            DemographicDataServiceURL = "ParticipantDemographicDataServiceURL",
            ServiceBusConnectionString_client_internal = "ServiceBusConnectionString_client_internal",
            ParticipantManagementTopic = "update-participant-queue",
            nemsmeshfolder_STORAGE = "BlobStorage_ConnectionString"
        };

        _config.Setup(c => c.Value).Returns(testConfig);

        Environment.SetEnvironmentVariable("NemsPoisonContainer", "nems-poison");

        _sut = new ProcessNemsUpdate(
            _loggerMock.Object,
            _fhirPatientDemographicMapperMock.Object,
            _createBasicParticipantDataMock.Object,
            _addBatchToQueueMock.Object,
            _httpClientFunctionMock.Object,
            _exceptionHandlerMock.Object,
            _participantDemographicMock.Object,
            _config.Object,
            _blobStorageHelperMock.Object
        );

        _httpClientFunctionMock.Reset();
        _fhirPatientDemographicMapperMock.Reset();
        _blobStorageHelperMock.Reset();
        _addBatchToQueueMock.Reset();
        _participantDemographicMock.Reset();

        // Default: simulate successful poison copy
        _blobStorageHelperMock
            .Setup(x => x.CopyFileToPoisonAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _fhirPatientDemographicMapperMock.Setup(x => x.ParseFhirJsonNhsNumber(It.IsAny<string>())).Returns(_validNhsNumber);

        _httpClientFunctionMock.Setup(x => x.SendGet("RetrievePdsDemographic", It.IsAny<Dictionary<string, string>>()))
            .ReturnsAsync(JsonSerializer.Serialize(new PdsDemographic() { NhsNumber = _validNhsNumber }));

        _httpClientFunctionMock.Setup(x => x.SendPost("Unsubscribe", It.IsAny<string>()))
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

        _httpClientFunctionMock.Verify(x => x.SendGet("RetrievePdsDemographic", It.IsAny<Dictionary<string, string>>()), Times.Never);

        _loggerMock.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v != null && v.ToString().Contains("There was an error getting the NHS number from the file.")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        Times.Once);

        // Verify poison copy occurs due to null NHS number handling
        _blobStorageHelperMock.Verify(x => x.CopyFileToPoisonAsync(
            "BlobStorage_ConnectionString",
            _fileName,
            "nems-updates",
            "nems-poison"), Times.Once);
    }

    [TestMethod]
    public async Task Run_PdsReturns404_CopiesFileToPoison_AndStopsProcessing()
    {
        // Arrange
        string fhirJson = LoadTestJson("mock-patient");
        await using var fileStream = File.OpenRead(fhirJson);

        // Return 404 from PDS
        var notFoundResponse = new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent("{}")
        };
        _httpClientFunctionMock
            .Setup(x => x.SendGetResponse(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
            .ReturnsAsync(notFoundResponse);

        // Act
        await _sut.Run(fileStream, _fileName);

        // Assert: poison copy invoked
        _blobStorageHelperMock.Verify(x => x.CopyFileToPoisonAsync(
            "BlobStorage_ConnectionString",
            _fileName,
            "nems-updates",
            "nems-poison"), Times.Once);

        // Assert: early return means no queueing, no unsubscribe
        _addBatchToQueueMock.Verify(x => x.ProcessBatch(It.IsAny<ConcurrentQueue<BasicParticipantCsvRecord>>(), It.IsAny<string>()), Times.Never);
        _httpClientFunctionMock.Verify(x => x.SendPost("Unsubscribe", It.IsAny<string>()), Times.Never);

        // Log indicates 404 handled
        _loggerMock.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v != null && v.ToString().Contains("the PDS function has returned a 404 error for file")),
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

        // _httpClientFunction.GetPDSRecord(_config.RetrievePdsDemographicURL, queryParams);
        HttpResponseMessage httpResponseMessage = new HttpResponseMessage();
        httpResponseMessage.StatusCode = HttpStatusCode.OK;

        _httpClientFunctionMock.Setup(x => x.SendGetResponse(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>())).ThrowsAsync(new Exception("error"));
        
        // No setup required for poison copy; we verify invocation

        // Act
        await _sut.Run(fileStream, _fileName);

        // Assert
        _fhirPatientDemographicMapperMock.Verify(x => x.ParseFhirJsonNhsNumber(It.IsAny<string>()), Times.Once);

        _httpClientFunctionMock.Verify(x => x.SendGetResponse("RetrievePdsDemographic", It.IsAny<Dictionary<string, string>>()), Times.Once);

        _loggerMock.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v != null && v.ToString().Contains("There was an error processing NEMS update for file")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        Times.Once);
    }

    [TestMethod]
    public async Task Run_NhsNumberFromNemsUpdateFileDoesNotMatchRetrievedPdsRecordNhsNumber_ProcessesRecordAndUnsubscribesFromNems()
    {
        // Arrange
        string fhirJson = LoadTestJson("mock-patient");
        await using var fileStream = File.OpenRead(fhirJson);

        HttpResponseMessage httpResponseMessage = new HttpResponseMessage();
        httpResponseMessage.Content = new StringContent(JsonSerializer.Serialize(new PdsDemographic { NhsNumber = "123" }));
        httpResponseMessage.StatusCode = HttpStatusCode.OK;

        _httpClientFunctionMock.Setup(x => x.SendGetResponse(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>())).ReturnsAsync(httpResponseMessage);

        // Act
        await _sut.Run(fileStream, _fileName);

        // Assert
        _fhirPatientDemographicMapperMock.Verify(x => x.ParseFhirJsonNhsNumber(It.IsAny<string>()), Times.Once);

        _httpClientFunctionMock.Verify(x => x.SendGetResponse("RetrievePdsDemographic", It.IsAny<Dictionary<string, string>>()), Times.Once);

        _loggerMock.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v != null && v.ToString().Contains("NHS numbers do not match, processing the superseded record.")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        Times.Once);

        _loggerMock.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v != null && v.ToString().Contains("Successfully unsubscribed from NEMS.")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        Times.Once);

        _httpClientFunctionMock.Verify(x => x.SendPost("Unsubscribe", It.IsAny<string>()), Times.Once);

        _addBatchToQueueMock.Verify(queue => queue.ProcessBatch(It.IsAny<ConcurrentQueue<BasicParticipantCsvRecord>>(), It.IsAny<string>()), Times.Once);
    }

    [TestMethod]
    public async Task Run_NhsNumberFromNemsUpdateFileDoesNotMatchRetrievedPdsRecordNhsNumberFailsUnsubscription_ProcessesRecord()
    {
        // Arrange
        string fhirJson = LoadTestJson("mock-patient");
        await using var fileStream = File.OpenRead(fhirJson);

        HttpResponseMessage httpResponseMessage = new HttpResponseMessage();
        httpResponseMessage.Content = new StringContent(JsonSerializer.Serialize(new PdsDemographic { NhsNumber = "123" }));
        httpResponseMessage.StatusCode = HttpStatusCode.OK;

        _httpClientFunctionMock.Setup(x => x.SendGetResponse(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>())).ReturnsAsync(httpResponseMessage);

        _httpClientFunctionMock.Setup(x => x.SendPost("Unsubscribe", It.IsAny<string>())).Throws(new Exception("error"));

        // Act
        await _sut.Run(fileStream, _fileName);

        // Assert
        _fhirPatientDemographicMapperMock.Verify(x => x.ParseFhirJsonNhsNumber(It.IsAny<string>()), Times.Once);

        _httpClientFunctionMock.Verify(x => x.SendGetResponse("RetrievePdsDemographic", It.IsAny<Dictionary<string, string>>()), Times.Once);

        _loggerMock.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v != null && v.ToString().Contains("NHS numbers do not match, processing the superseded record.")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        Times.Once);

        _loggerMock.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v != null && v.ToString().Contains("Successfully unsubscribed from NEMS.")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        Times.Never);

        _httpClientFunctionMock.Verify(x => x.SendPost("Unsubscribe", It.IsAny<string>()), Times.Once);

        _addBatchToQueueMock.Verify(queue => queue.ProcessBatch(It.IsAny<ConcurrentQueue<BasicParticipantCsvRecord>>(), It.IsAny<string>()), Times.Once);
    }

    [TestMethod]
    public async Task Run_NemsUpdateMatchesPdsRecord_ProcessesRecord()
    {
        // Arrange
        string fhirJson = LoadTestJson("mock-patient");
        await using var fileStream = File.OpenRead(fhirJson);

        HttpResponseMessage httpResponseMessage = new HttpResponseMessage();
        httpResponseMessage.Content = new StringContent(JsonSerializer.Serialize(new PdsDemographic { NhsNumber = "9000000009" }));
        httpResponseMessage.StatusCode = HttpStatusCode.OK;

        _httpClientFunctionMock.Setup(x => x.SendGetResponse(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>())).ReturnsAsync(httpResponseMessage);

        // Act
        await _sut.Run(fileStream, _fileName);

        // Assert
        _fhirPatientDemographicMapperMock.Verify(x => x.ParseFhirJsonNhsNumber(It.IsAny<string>()), Times.Once);

        _httpClientFunctionMock.Verify(x => x.SendGetResponse("RetrievePdsDemographic", It.IsAny<Dictionary<string, string>>()), Times.Once);


        _loggerMock.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v != null && v.ToString().Contains("NHS numbers match, processing the retrieved PDS record.")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        Times.Once);

        _addBatchToQueueMock.Verify(queue => queue.ProcessBatch(It.IsAny<ConcurrentQueue<BasicParticipantCsvRecord>>(), It.IsAny<string>()), Times.Once);
    }



    [TestMethod]
    public async Task Run_NhsNumberFromNemsUpdateFileDoesNotMatchRetrievedPdsRecordNhsNumber_ProcessesRecord_RaiseInfoExceptionAndUnsubscribesFromNems()
    {
        // Arrange
        string fhirJson = LoadTestJson("mock-patient");
        await using var fileStream = File.OpenRead(fhirJson);

        const string supersededNhsNumber = "123";

        HttpResponseMessage httpResponseMessage = new HttpResponseMessage();
        httpResponseMessage.Content = new StringContent(JsonSerializer.Serialize(new PdsDemographic { NhsNumber = "123" }));
        httpResponseMessage.StatusCode = HttpStatusCode.OK;

        _httpClientFunctionMock.Setup(x => x.SendGetResponse(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>())).ReturnsAsync(httpResponseMessage);

        // Act
        await _sut.Run(fileStream, _fileName);

        // Assert
        _fhirPatientDemographicMapperMock.Verify(x => x.ParseFhirJsonNhsNumber(It.IsAny<string>()), Times.Once);
        _httpClientFunctionMock.Verify(x => x.SendGetResponse("RetrievePdsDemographic", It.IsAny<Dictionary<string, string>>()), Times.Once);



        _loggerMock.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v != null && v.ToString().Contains("NHS numbers do not match, processing the superseded record.")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        Times.Once);

        _loggerMock.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v != null && v.ToString().Contains("Successfully unsubscribed from NEMS.")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        Times.Once);

        _httpClientFunctionMock.Verify(x => x.SendPost("Unsubscribe", It.IsAny<string>()), Times.Once);
        _addBatchToQueueMock.Verify(queue => queue.ProcessBatch(It.IsAny<ConcurrentQueue<BasicParticipantCsvRecord>>(), It.IsAny<string>()), Times.Once);

        //Verify the exception handler was called
        _exceptionHandlerMock.Verify(
        x => x.CreateTransformExecutedExceptions(
        It.Is<CohortDistributionParticipant>(p =>
            p.NhsNumber == _validNhsNumber &&
            p.SupersededByNhsNumber == supersededNhsNumber),
        "SupersededNhsNumber",
        60,
        null),
        Times.Once);
    }

    [TestMethod]
    public async Task Run_XmlFileExtension_CallsXmlParser()
    {
        // Arrange
        string xmlFileName = "test-file.xml";
        string fhirXml = "<test>xml content</test>";
        await using var fileStream = new MemoryStream(Encoding.UTF8.GetBytes(fhirXml));

        // Act
        await _sut.Run(fileStream, xmlFileName);

        // Assert
        _fhirPatientDemographicMapperMock.Verify(x => x.ParseFhirXmlNhsNumber(It.IsAny<string>()), Times.Once);
        _fhirPatientDemographicMapperMock.Verify(x => x.ParseFhirJsonNhsNumber(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task Run_JsonFileExtension_CallsJsonParser()
    {
        // Arrange
        string jsonFileName = "test-file.json";
        string fhirJson = LoadTestJson("mock-patient");
        await using var fileStream = File.OpenRead(fhirJson);

        // Act
        await _sut.Run(fileStream, jsonFileName);

        // Assert
        _fhirPatientDemographicMapperMock.Verify(x => x.ParseFhirJsonNhsNumber(It.IsAny<string>()), Times.Once);
        _fhirPatientDemographicMapperMock.Verify(x => x.ParseFhirXmlNhsNumber(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task Run_ExtractedNhsNumber_PassedToPdsService()
    {
        // Arrange
        const string expectedNhsNumber = "9000000009";
        string fhirJson = LoadTestJson("mock-patient");
        await using var fileStream = File.OpenRead(fhirJson);

        _fhirPatientDemographicMapperMock.Setup(x => x.ParseFhirJsonNhsNumber(It.IsAny<string>()))
            .Returns(expectedNhsNumber);

        // Act
        await _sut.Run(fileStream, _fileName);

        // Assert - Verify correct NHS number is passed to PDS service
        _httpClientFunctionMock.Verify(x => x.SendGetResponse(
            "RetrievePdsDemographic",
            It.Is<Dictionary<string, string>>(dict =>
                dict.ContainsKey("nhsNumber") && dict["nhsNumber"] == expectedNhsNumber)),
            Times.Once);
    }

    [TestMethod]
    public async Task Run_XmlBundleFile_PassesNhsNumberToPdsService()
    {
        // Arrange
        const string expectedNhsNumber = "9000000009";
        string xmlFileName = "nems-bundle.xml";
        string xmlContent = LoadTestXml("nems-bundle");
        await using var fileStream = new MemoryStream(Encoding.UTF8.GetBytes(xmlContent));

        _fhirPatientDemographicMapperMock.Setup(x => x.ParseFhirXmlNhsNumber(It.IsAny<string>()))
            .Returns(expectedNhsNumber);

        // Act
        await _sut.Run(fileStream, xmlFileName);

        // Assert - Verify correct NHS number is passed to PDS service
        _httpClientFunctionMock.Verify(x => x.SendGetResponse(
            "RetrievePdsDemographic",
            It.Is<Dictionary<string, string>>(dict =>
                dict.ContainsKey("nhsNumber") && dict["nhsNumber"] == expectedNhsNumber)),
            Times.Once);
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

    private static string LoadTestXml(string filename)
    {
        // Add .xml extension if not already present
        string filenameWithExtension = filename.EndsWith(".xml", StringComparison.OrdinalIgnoreCase)
            ? filename
            : $"{filename}.xml";

        // Get the directory of the currently executing assembly
        string assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
        string assemblyDirectory = Path.GetDirectoryName(assemblyLocation) ?? string.Empty;

        // Try the SharedTests path for XML files
        string sharedTestsPath = Path.Combine(assemblyDirectory, "../../../SharedTests/FhirPatientDemographicMapperTests/PatientMocks", filenameWithExtension);
        if (File.Exists(sharedTestsPath))
        {
            return File.ReadAllText(sharedTestsPath);
        }

        // Try the original path
        string originalPath = Path.Combine(assemblyDirectory, "../../../PatientMocks", filenameWithExtension);
        if (File.Exists(originalPath))
        {
            return File.ReadAllText(originalPath);
        }

        // If neither path exists, throw an exception
        throw new FileNotFoundException($"Test JSON file '{filenameWithExtension}' not found in either expected location.");
    }

    [TestMethod]
    public async Task Run_AddBatchToQueueFails_CopiesFileToPoisonContainer()
    {
        // Arrange
        string fhirJson = LoadTestJson("mock-patient");
        Stream fileStream;
        if (!string.IsNullOrEmpty(fhirJson) && File.Exists(fhirJson))
            fileStream = File.OpenRead(fhirJson);
        else
            fileStream = new MemoryStream(Encoding.UTF8.GetBytes("{\"resourceType\":\"Patient\"}"));
        // Ensure we reach ProcessRecord by setting up valid responses
        _httpClientFunctionMock.Setup(x => x.SendGet("RetrievePdsDemographic", It.IsAny<Dictionary<string, string>>()))
            .ReturnsAsync(JsonSerializer.Serialize(new PdsDemographic() { NhsNumber = _validNhsNumber }));
        // Throw exception in AddBatchToQueue to trigger poison container
        _addBatchToQueueMock.Setup(x => x.ProcessBatch(It.IsAny<ConcurrentQueue<BasicParticipantCsvRecord>>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("Queue error"));
        // No setup needed; we assert CopyFileToPoisonAsync is called
        // Act
        await _sut.Run(fileStream, _fileName);
        // Assert
        _blobStorageHelperMock.Verify(x => x.CopyFileToPoisonAsync(
            "BlobStorage_ConnectionString",
            _fileName,
            "nems-updates",
            "nems-poison"), Times.Once);
        _loggerMock.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v != null && v.ToString().Contains("There was an error processing NEMS update for file")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        Times.Once);
        _loggerMock.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v != null && v.ToString().Contains("Copied failed NEMS file")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        Times.Once);
        _loggerMock.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v != null && v.ToString().Contains("Failed to copy NEMS file")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        Times.AtMostOnce());
        await fileStream.DisposeAsync();
    }

    [TestMethod]
    public async Task Run_InvalidNhsNumberValidation_CopiesFileToPoisonContainer()
    {
        // Arrange
        string fhirJson = LoadTestJson("mock-patient");
        Stream fileStream;
        if (!string.IsNullOrEmpty(fhirJson) && File.Exists(fhirJson))
            fileStream = File.OpenRead(fhirJson);
        else
            fileStream = new MemoryStream(Encoding.UTF8.GetBytes("{\"resourceType\":\"Patient\"}"));
        // Return an invalid NHS number that will fail ValidationHelper.ValidateNHSNumber
        _fhirPatientDemographicMapperMock.Setup(x => x.ParseFhirJsonNhsNumber(It.IsAny<string>()))
            .Returns("123456789"); // Invalid NHS number format
        // Act
        await _sut.Run(fileStream, _fileName);
        // Assert
        _blobStorageHelperMock.Verify(x => x.CopyFileToPoisonAsync(
            "BlobStorage_ConnectionString",
            _fileName,
            "nems-updates",
            "nems-poison"), Times.Once);
        _loggerMock.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v != null && v.ToString().Contains("There was an error processing NEMS update for file")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        Times.Once);
        await fileStream.DisposeAsync();
    }

    [TestMethod]
    public async Task Run_PoisonContainerUploadFails_LogsError()
    {
        // Arrange
        string fhirJson = LoadTestJson("mock-patient");
        Stream fileStream;
        if (!string.IsNullOrEmpty(fhirJson) && File.Exists(fhirJson))
            fileStream = File.OpenRead(fhirJson);
        else
            fileStream = new MemoryStream(Encoding.UTF8.GetBytes("{\"resourceType\":\"Patient\"}"));
        // Trigger exception in JSON deserialization
        _httpClientFunctionMock.Setup(x => x.SendGet("RetrievePdsDemographic", It.IsAny<Dictionary<string, string>>()))
            .ReturnsAsync("invalid-json");
        _blobStorageHelperMock
            .Setup(x => x.CopyFileToPoisonAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ThrowsAsync(new Exception("copy failed"));
        // Act
        await _sut.Run(fileStream, _fileName);
        // Assert
        _blobStorageHelperMock.Verify(x => x.CopyFileToPoisonAsync(
            "BlobStorage_ConnectionString",
            _fileName,
            "nems-updates",
            "nems-poison"), Times.Once);
        _loggerMock.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v != null && v.ToString().Contains("There was an error processing NEMS update for file")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        Times.Once);
        _loggerMock.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v != null && v.ToString().Contains("Failed to copy NEMS file")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        Times.Once);
        await fileStream.DisposeAsync();
    }

    // Timestamp-based rename not applicable for poison copy (server-side copy retains original name)

    [TestMethod]
    public async Task Run_DataServiceClientThrowsException_CopiesFileToPoisonContainer()
    {
        // Arrange
        string fhirJson = LoadTestJson("mock-patient");
        Stream fileStream;
        if (!string.IsNullOrEmpty(fhirJson) && File.Exists(fhirJson))
            fileStream = File.OpenRead(fhirJson);
        else
            fileStream = new MemoryStream(Encoding.UTF8.GetBytes("{\"resourceType\":\"Patient\"}"));
        // Setup so that DataServiceClient throws
        _httpClientFunctionMock.Setup(x => x.SendGet("RetrievePdsDemographic", It.IsAny<Dictionary<string, string>>()))
            .ReturnsAsync(JsonSerializer.Serialize(new PdsDemographic() { NhsNumber = _validNhsNumber }));
        _participantDemographicMock.Setup(x => x.GetSingleByFilter(It.IsAny<Expression<Func<ParticipantDemographic, bool>>>()))
            .ThrowsAsync(new Exception("DataServiceClient error"));
        // No setup needed for poison copy
        // Act
        await _sut.Run(fileStream, _fileName);
        // Assert
        _blobStorageHelperMock.Verify(x => x.CopyFileToPoisonAsync(
            "BlobStorage_ConnectionString",
            _fileName,
            "nems-updates",
            "nems-poison"), Times.Once);
        _loggerMock.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v != null && v.ToString().Contains("There was an error processing NEMS update for file")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        Times.Once);
        await fileStream.DisposeAsync();
    }

    [TestMethod]
    public async Task Run_SuccessfulProcessing_DoesNotCallPoisonContainer()
    {
        // Arrange
        string fhirJson = LoadTestJson("mock-patient");
        await using var fileStream = File.OpenRead(fhirJson);

        // Setup successful PDS response that matches the NHS number
        HttpResponseMessage httpResponseMessage = new HttpResponseMessage();
        httpResponseMessage.Content = new StringContent(JsonSerializer.Serialize(new PdsDemographic { NhsNumber = "9000000009" }));
        httpResponseMessage.StatusCode = HttpStatusCode.OK;

        _httpClientFunctionMock.Setup(x => x.SendGetResponse(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>())).ReturnsAsync(httpResponseMessage);

        // Act
        await _sut.Run(fileStream, _fileName);

        // Assert - Verify poison container is never called on successful processing
        _blobStorageHelperMock.Verify(x => x.CopyFileToPoisonAsync(
            It.IsAny<string>(), 
            It.IsAny<string>(), 
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Never);

        _loggerMock.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v != null && v.ToString().Contains("There was an error processing NEMS update for file")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        Times.Never);
        
        // Verify successful processing occurred
        _addBatchToQueueMock.Verify(x => x.ProcessBatch(It.IsAny<ConcurrentQueue<BasicParticipantCsvRecord>>(), It.IsAny<string>()), Times.Once);
    }
}
