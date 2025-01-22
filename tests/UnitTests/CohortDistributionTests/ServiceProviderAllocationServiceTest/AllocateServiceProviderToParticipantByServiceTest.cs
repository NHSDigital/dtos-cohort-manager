namespace NHS.CohortManager.Tests.UnitTests.ServiceProviderAllocationServiceTests;

using System.Net;
using System.Text.Json;
using Common;
using Moq;
using NHS.CohortManager.ServiceProviderAllocationService;
using NHS.CohortManager.Tests.TestUtils;

[TestClass]
public class AllocateServiceProviderToParticipantByServiceTests : DatabaseTestBaseSetup<AllocateServiceProviderToParticipantByService>
{
    private static readonly Mock<IExceptionHandler> _exceptionHandler = new();
    private AllocationConfigRequestBody _cohortDistributionData = new();
    private string? _configFilePath;

    public AllocateServiceProviderToParticipantByServiceTests() : base((conn, logger, transaction, command, response) =>
    new AllocateServiceProviderToParticipantByService(logger, response, _exceptionHandler.Object))
    {
        Environment.SetEnvironmentVariable("CreateValidationExceptionURL", "CreateValidationExceptionURL");
        SetupConfigFile();
        CreateHttpResponseMock();
    }

    [TestInitialize]
    public void TestInitialize()
    {
        _exceptionHandler.Reset();
        _service = new AllocateServiceProviderToParticipantByService(
            _loggerMock.Object,
            _createResponseMock.Object,
            _exceptionHandler.Object);
    }

    [TestMethod]
    public async Task Run_CorrectAllocationInformation_ReturnsSuccessAndServiceProvider()
    {
        //Arrange
        _cohortDistributionData = new AllocationConfigRequestBody
        {
            NhsNumber = "1234567890",
            Postcode = "NE63",
            ScreeningAcronym = "BSS"
        };

        var allocationData = JsonSerializer.Serialize(_cohortDistributionData);
        _request = SetupRequest(allocationData);

        // Act
        var result = await _service.Run(_request.Object);
        string responseBody = await AssertionHelper.ReadResponseBodyAsync(result);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        Assert.AreEqual("BS Select - NE63", responseBody);
        _exceptionHandler.Verify(x => x.CreateSystemExceptionLogFromNhsNumber(It.IsAny<Exception>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never());
    }

    [TestMethod]
    [DataRow("1234567890", null, null)]
    [DataRow("1234567890", null, "BSS")]
    [DataRow("1234567890", "NE63", null)]
    public async Task Run_MissingRequiredData_ReturnsBadRequestAndCreateSystemExceptionLog(
    string nhsNumber,
    string postcode,
    string screeningAcronym)
    {
        //Arrange
        _cohortDistributionData = new AllocationConfigRequestBody
        {
            NhsNumber = nhsNumber,
            Postcode = postcode,
            ScreeningAcronym = screeningAcronym
        };
        var allocationData = JsonSerializer.Serialize(_cohortDistributionData);
        _request = SetupRequest(allocationData);

        // Act
        var result = await _service.Run(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
        _exceptionHandler.Verify(x => x.CreateSystemExceptionLogFromNhsNumber(
            It.IsAny<Exception>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()),
            Times.Once());
    }

    [TestMethod]
    public async Task Run_NoMatchingEntryFound_ReturnsOkWithDefaultServiceProvider()
    {
        //Arrange
        _cohortDistributionData = new AllocationConfigRequestBody
        {
            NhsNumber = "1234567890",
            Postcode = "ZX",
            ScreeningAcronym = "BSS"
        };
        var allocationData = JsonSerializer.Serialize(_cohortDistributionData);
        _request = SetupRequest(allocationData);

        // Act
        var result = await _service.Run(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        Assert.AreEqual("BS SELECT", await AssertionHelper.ReadResponseBodyAsync(result));
    }

    [TestMethod]
    public async Task Run_ConfigFileNotFound_ReturnsBadRequestAndCreateSystemExceptionLogFromNhsNumber()
    {
        //Arrange
        _cohortDistributionData = new AllocationConfigRequestBody
        {
            NhsNumber = "1234567890",
            Postcode = "NE63",
            ScreeningAcronym = "BSS"
        };
        var allocationData = JsonSerializer.Serialize(_cohortDistributionData);
        _request = SetupRequest(allocationData);

        var currentConfigPath = Path.Combine(Environment.CurrentDirectory, "ConfigFiles", "allocationConfig.json");
        if (File.Exists(currentConfigPath))
        {
            File.Delete(currentConfigPath);
        }
        var configDir = Path.GetDirectoryName(currentConfigPath);
        if (Directory.Exists(configDir))
        {
            Directory.Delete(configDir, true);
        }

        // Act
        var result = await _service.Run(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
        _exceptionHandler.Verify(x => x.CreateSystemExceptionLogFromNhsNumber(
            It.Is<Exception>(e => e.Message.Contains("Cannot find allocation configuration file")),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()),
            Times.Once());
    }

    [TestMethod]
    public async Task Run_ExceptionThrown_IsCaughtReturnsInternalServerErrorAndCreateSystemExceptionLogFromNhsNumber()
    {
        //Arrange
        SetupRequest(string.Empty);
        _request.Setup(r => r.Body).Throws(new Exception("Test Exception"));

        // Act
        var result = await _service.Run(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
        _exceptionHandler.Verify(x => x.CreateSystemExceptionLogFromNhsNumber(
            It.Is<Exception>(e => e.Message == "Test Exception"),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()),
            Times.Once());
    }

    private void SetupConfigFile()
    {
        var configDir = Path.Combine(Environment.CurrentDirectory, "ConfigFiles");
        Directory.CreateDirectory(configDir);
        _configFilePath = Path.Combine(configDir, "allocationConfig.json");

        var configContent = new AllocationConfigDataList
        {
            ConfigDataList =
            [
                new AllocationConfigData
                {
                    Postcode = "NE63",
                    ScreeningService = "BSS",
                    ServiceProvider = "BS Select - NE63"
                }
            ]
        };
        File.WriteAllText(_configFilePath, JsonSerializer.Serialize(configContent));
    }
}
