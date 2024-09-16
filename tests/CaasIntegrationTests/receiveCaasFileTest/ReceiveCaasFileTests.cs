namespace NHS.CohortManager.Tests.CaasIntegrationTests;

using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Common;
using NHS.Screening.ReceiveCaasFile;
using Data.Database;
using Model;

[TestClass]
public class ReceiveCaasFileTests
{
    private readonly Mock<ILogger<ReceiveCaasFile>> _mockLogger;
    private readonly Mock<ICallFunction> _mockICallFunction;
    private readonly Mock<IFileReader> _mockIFileReader;
    private readonly ReceiveCaasFile _receiveCaasFileInstance;
    private readonly string _validCsvData;
    private readonly string _invalidCsvData;
    private string _expectedJson;
    private readonly string _blobName;
    private readonly Mock<HttpWebResponse> _webResponse = new();
    private readonly Mock<IScreeningServiceData> _screeningServiceData = new();

    public ReceiveCaasFileTests()
    {
        _mockLogger = new Mock<ILogger<ReceiveCaasFile>>();
        _mockICallFunction = new Mock<ICallFunction>();
        _mockIFileReader = new Mock<IFileReader>();
        _receiveCaasFileInstance = new ReceiveCaasFile(_mockLogger.Object, _mockICallFunction.Object, _screeningServiceData.Object);
        _blobName = "BSS_20240718150245_n3.parquet";
        _expectedJson = "{\"Participants\":[{\"RecordType\":\"New\",\"ChangeTimeStamp\":\"20240524153000\",\"SerialChangeNumber\":\"1\",\"NhsNumber\":\"1111111111\",\"SupersededByNhsNumber\":\"\",\"PrimaryCareProvider\":\"B83006\",\"PrimaryCareProviderEffectiveFromDate\":\"20240410\",\"CurrentPosting\":\"Manchester\",\"CurrentPostingEffectiveFromDate\":\"20240410\",\"NamePrefix\":\"Mr\",\"FirstName\":\"Joe\",\"OtherGivenNames\":null,\"Surname\":\"Bloggs\",\"PreviousSurname\":null,\"DateOfBirth\":\"19711221\",\"Gender\":1,\"AddressLine1\":\"HEXAGON HOUSE\",\"AddressLine2\":\"PYNES HILL\",\"AddressLine3\":\"RYDON LANE\",\"AddressLine4\":\"EXETER\",\"AddressLine5\":\"DEVON\",\"Postcode\":\"BV3 9ZA\",\"PafKey\":\"1234\",\"UsualAddressEffectiveFromDate\":null,\"ReasonForRemoval\":null,\"ReasonForRemovalEffectiveFromDate\":null,\"DateOfDeath\":null,\"DeathStatus\":null,\"TelephoneNumber\":null,\"TelephoneNumberEffectiveFromDate\":null,\"MobileNumber\":null,\"MobileNumberEffectiveFromDate\":null,\"EmailAddress\":null,\"EmailAddressEffectiveFromDate\":null,\"PreferredLanguage\":\"English\",\"IsInterpreterRequired\":\"0\",\"InvalidFlag\":\"0\",\"ParticipantId\":null,\"ScreeningId\":null,\"BusinessRuleVersion\":null,\"ExceptionFlag\":null,\"RecordInsertDateTime\":null,\"RecordUpdateDateTime\":null,\"ScreeningAcronym\":null,\"ScreeningName\":null},{\"RecordType\":\"Amended\",\"ChangeTimeStamp\":\"20240524153000\",\"SerialChangeNumber\":\"2\",\"NhsNumber\":\"2222222222\",\"SupersededByNhsNumber\":\"\",\"PrimaryCareProvider\":\"D81026\",\"PrimaryCareProviderEffectiveFromDate\":\"20240411\",\"CurrentPosting\":\"Liverpool\",\"CurrentPostingEffectiveFromDate\":\"20240411\",\"NamePrefix\":\"Mrs\",\"FirstName\":\"Jane\",\"OtherGivenNames\":null,\"Surname\":\"Doe\",\"PreviousSurname\":null,\"DateOfBirth\":\"19680801\",\"Gender\":2,\"AddressLine1\":\"1 New Road\",\"AddressLine2\":\"SOLIHULL\",\"AddressLine3\":\"West Midlands\",\"AddressLine4\":null,\"AddressLine5\":null,\"Postcode\":\"B91 3DL\",\"PafKey\":\"4321\",\"UsualAddressEffectiveFromDate\":null,\"ReasonForRemoval\":null,\"ReasonForRemovalEffectiveFromDate\":null,\"DateOfDeath\":\"20240501\",\"DeathStatus\":1,\"TelephoneNumber\":null,\"TelephoneNumberEffectiveFromDate\":null,\"MobileNumber\":null,\"MobileNumberEffectiveFromDate\":null,\"EmailAddress\":null,\"EmailAddressEffectiveFromDate\":null,\"PreferredLanguage\":\"English\",\"IsInterpreterRequired\":\"0\",\"InvalidFlag\":\"0\",\"ParticipantId\":null,\"ScreeningId\":null,\"BusinessRuleVersion\":null,\"ExceptionFlag\":null,\"RecordInsertDateTime\":null,\"RecordUpdateDateTime\":null,\"ScreeningAcronym\":null,\"ScreeningName\":null},{\"RecordType\":\"Removed\",\"ChangeTimeStamp\":\"20240524153000\",\"SerialChangeNumber\":\"3\",\"NhsNumber\":\"3333333333\",\"SupersededByNhsNumber\":\"\",\"PrimaryCareProvider\":\"L83137\",\"PrimaryCareProviderEffectiveFromDate\":\"20240412\",\"CurrentPosting\":\"London\",\"CurrentPostingEffectiveFromDate\":\"20240412\",\"NamePrefix\":\"Dr\",\"FirstName\":\"John\",\"OtherGivenNames\":null,\"Surname\":\"Jones\",\"PreviousSurname\":null,\"DateOfBirth\":\"19501201\",\"Gender\":1,\"AddressLine1\":\"100\",\"AddressLine2\":\"spen lane\",\"AddressLine3\":\"Leeds\",\"AddressLine4\":null,\"AddressLine5\":null,\"Postcode\":\"LS16 5BR\",\"PafKey\":\"5555\",\"UsualAddressEffectiveFromDate\":null,\"ReasonForRemoval\":null,\"ReasonForRemovalEffectiveFromDate\":null,\"DateOfDeath\":null,\"DeathStatus\":null,\"TelephoneNumber\":null,\"TelephoneNumberEffectiveFromDate\":null,\"MobileNumber\":null,\"MobileNumberEffectiveFromDate\":null,\"EmailAddress\":null,\"EmailAddressEffectiveFromDate\":null,\"PreferredLanguage\":\"French\",\"IsInterpreterRequired\":\"1\",\"InvalidFlag\":\"0\",\"ParticipantId\":null,\"ScreeningId\":null,\"BusinessRuleVersion\":null,\"ExceptionFlag\":null,\"RecordInsertDateTime\":null,\"RecordUpdateDateTime\":null,\"ScreeningAcronym\":null,\"ScreeningName\":null}],\"FileName\":\"BSS_20240718150245_n3.parquet\"}";

        _invalidCsvData = "invalid data";
    }

    [TestMethod]
    public async Task Run_SuccessfulParseAndSendDataWithValidInput_SuccessfulSendToFunctionWithResponse()
    {
        // Arrange

        using (var fileSteam = File.OpenRead("BSS_20240718150245_n3.parquet"))
        {

            _mockICallFunction.Setup(callFunction => callFunction.SendPost(It.IsAny<string>(), It.IsAny<string>())).Verifiable();
            _screeningServiceData.Setup(x => x.GetScreeningServiceByAcronym(It.IsAny<string>())).Returns(new ScreeningService());

            // Act
            await _receiveCaasFileInstance.Run(fileSteam, _blobName);

            // Assert
            _mockLogger.Verify(
                m => m.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
                Times.AtLeastOnce,
                "No logging received."
            );

            _mockICallFunction.Verify(
                x => x.SendPost(It.IsAny<string>(),
                It.Is<string>(json => json.Contains(_expectedJson))),
                Times.Once);
        }
    }

    [TestMethod]
    public async Task Run_SuccessfulParseWithInvalidInput_FailsAndLogsError()
    {
        // Arrange
        Environment.SetEnvironmentVariable("FileValidationURL", "FileValidationURL");
        _mockICallFunction.Setup(callFunction => callFunction.SendPost(It.IsAny<string>(), It.IsAny<string>())).Verifiable();
        _screeningServiceData.Setup(x => x.GetScreeningServiceByAcronym(It.IsAny<string>())).Returns(new ScreeningService());

        _webResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.OK);

        _mockICallFunction.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("FileValidationURL")), It.IsAny<string>()))
                            .Returns(Task.FromResult<HttpWebResponse>(_webResponse.Object));
        using (var fileSteam = File.OpenRead("BSS_20240718150245_n3.parquet"))
        {
            // Act
            await _receiveCaasFileInstance.Run(fileSteam, "BSS_20240718150245_n30.parquet");

            // Assert
            _mockLogger.Verify(x => x.Log(It.Is<LogLevel>(l => l == LogLevel.Error),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("File name record count not equal to actual record count. File name count:")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);

            _mockICallFunction.Verify(
            x => x.SendPost(It.IsAny<string>(),
            It.Is<string>(json => json == _expectedJson)),
            Times.Never);

            _mockICallFunction.Verify(
            x => x.SendPost(It.Is<string>(url => url == "FileValidationURL"),
            It.IsAny<string>()),
            Times.Once);
        }
    }

    [TestMethod]
    public async Task Run_UnsuccessfulParseAndSendDataWithValidInput_FailsAndLogsError()
    {
        // Arrange
        _mockICallFunction.Setup(callFunction => callFunction.SendPost(It.IsAny<string>(), It.IsAny<string>())).Verifiable();
        _screeningServiceData.Setup(x => x.GetScreeningServiceByAcronym(It.IsAny<string>())).Returns(new ScreeningService());

        using (var fileSteam = File.OpenRead("BSS_20240718150245_n4.parquet"))
        {
            // Act
            await _receiveCaasFileInstance.Run(fileSteam, _blobName);

            // Assert
            _mockLogger.Verify(l => l.Log(LogLevel.Information,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.AtLeastOnce());
        }
    }

    [TestMethod]
    public async Task Run_InvalidFileExtension_LogsValidationErrorAndReturns()
    {
        // Arrange
        var fileName = "Test.PDF";
        Environment.SetEnvironmentVariable("FileValidationURL", "FileValidationURL");
        _mockICallFunction.Setup(callFunction => callFunction.SendPost(It.IsAny<string>(), It.IsAny<string>())).Verifiable();

        _webResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.OK);

        _mockICallFunction.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("FileValidationURL")), It.IsAny<string>()))
                            .Returns(Task.FromResult<HttpWebResponse>(_webResponse.Object));

        // Act & Assert
        using (var fileSteam = File.OpenRead("BSS_20240718150245_n3.parquet"))
        {
            await _receiveCaasFileInstance.Run(fileSteam, fileName);

            _mockLogger.Verify(x => x.Log(It.Is<LogLevel>(l => l == LogLevel.Error),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("File name or file extension is invalid. Not in format BSS_ccyymmddhhmmss_n8.parquet. file Name: ")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);

            _mockICallFunction.Verify(
            x => x.SendPost(It.Is<string>(url => url == "FileValidationURL"),
            It.IsAny<string>()),
            Times.Once);

            _mockICallFunction.Verify(
            x => x.SendPost(It.IsAny<string>(),
            It.Is<string>(json => json == _expectedJson)),
            Times.Never);
        }
    }

    [TestMethod]
    public async Task Run_ValidFileNameRecordCount_CompletesAsExpected()
    {
        // Arrange

        Environment.SetEnvironmentVariable("targetFunction", "targetFunction");
        _mockICallFunction.Setup(callFunction => callFunction.SendPost(It.IsAny<string>(), It.IsAny<string>())).Verifiable();
        _screeningServiceData.Setup(x => x.GetScreeningServiceByAcronym(It.IsAny<string>())).Returns(new ScreeningService());

        _webResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.OK);

        _mockICallFunction.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("targetFunction")), It.IsAny<string>()))
                            .Returns(Task.FromResult<HttpWebResponse>(_webResponse.Object));

        using (var fileSteam = File.OpenRead("BSS_20240718150245_n3.parquet"))
        {
            // Act
            await _receiveCaasFileInstance.Run(fileSteam, _blobName);

            // Assert
            _mockICallFunction.Verify(
            x => x.SendPost(It.IsAny<string>(),
            It.Is<string>(json => json == _expectedJson)),
            Times.Once);
        }
    }
}
