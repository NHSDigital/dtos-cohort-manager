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
using NHS.CohortManager.Tests.TestUtils;

[TestClass]
public class ReceiveCaasFileTests
{
    private readonly Mock<ILogger<ReceiveCaasFile>> _mockLogger;
    private readonly Mock<ICallFunction> _mockICallFunction;
    private readonly Mock<IFileReader> _mockIFileReader;
    private readonly Mock<IReceiveCaasFileHelper> _mockIReceiveCaasFileHelper;
    private readonly ReceiveCaasFile _receiveCaasFileInstance;
    private readonly Participant _participant;
    private readonly ParticipantsParquetMap _participantsParquetMap;
    private readonly Cohort _cohort;
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
        _mockIReceiveCaasFileHelper = new Mock<IReceiveCaasFileHelper>();
        _receiveCaasFileInstance = new ReceiveCaasFile(_mockLogger.Object, _mockIReceiveCaasFileHelper.Object, _screeningServiceData.Object);
        _blobName = "BSS_20241201121212_n1.parquet";
        _expectedJson = "{\"Participants\":[{\"RecordType\":\"ADD\",\"ChangeTimeStamp\":\"20240524000000\",\"SerialChangeNumber\":\"1\",\"NhsNumber\":\"1111122202\",\"SupersededByNhsNumber\":\"\",\"PrimaryCareProvider\":\"E85121\",\"PrimaryCareProviderEffectiveFromDate\":\"20030319\",\"CurrentPosting\":\"BD\",\"CurrentPostingEffectiveFromDate\":\"20130419\",\"NamePrefix\":\"Miss\",\"FirstName\":\"John\",\"OtherGivenNames\":\"Reymond\",\"FamilyName\":\"Regans\",\"PreviousFamilyName\":\"\",\"DateOfBirth\":\"19600112\",\"Gender\":1,\"AddressLine1\":\"25 Ring Road\",\"AddressLine2\":\"Eastend\",\"AddressLine3\":\"\",\"AddressLine4\":\"Ashford\",\"AddressLine5\":\"United Kingdom\",\"Postcode\":\"AB43 8JR\",\"PafKey\":\"Z3S4Q5X8\",\"UsualAddressEffectiveFromDate\":\"20031118\",\"ReasonForRemoval\":\"\",\"ReasonForRemovalEffectiveFromDate\":\"\",\"DateOfDeath\":\"\",\"DeathStatus\":null,\"TelephoneNumber\":\"1619999998\",\"TelephoneNumberEffectiveFromDate\":\"20200819\",\"MobileNumber\":\"7888888889\",\"MobileNumberEffectiveFromDate\":\"20240502\",\"EmailAddress\":null,\"EmailAddressEffectiveFromDate\":null,\"PreferredLanguage\":null,\"IsInterpreterRequired\":\"1\",\"InvalidFlag\":\"0\",\"ParticipantId\":null,\"ScreeningId\":null,\"BusinessRuleVersion\":null,\"ExceptionFlag\":null,\"RecordInsertDateTime\":null,\"RecordUpdateDateTime\":null,\"ScreeningAcronym\":null,\"ScreeningName\":null,\"EligibilityFlag\":null}],\"FileName\":\"BSS_20240718150245_n3.parquet\"}";
        _invalidCsvData = "invalid data";
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
        _mockICallFunction.Setup(callFunction => callFunction.SendPost(It.IsAny<string>(), It.IsAny<string>())).Verifiable();
        _screeningServiceData.Setup(x => x.GetScreeningServiceByAcronym(It.IsAny<string>())).Returns(new ScreeningService());
        _mockIReceiveCaasFileHelper.Setup(x => x.InitialChecks(fileSteam, _blobName)).Returns(Task.FromResult(true));
        _mockIReceiveCaasFileHelper.Setup(x => x.GetNumberOfRecordsFromFileName(_blobName)).Returns(Task.FromResult<int?>(1));
        _mockIReceiveCaasFileHelper.Setup(x => x.MapParticipant(_participantsParquetMap, _participant, _blobName, It.IsAny<int>())).Returns(Task.FromResult<Participant?>(_participant));
        _mockIReceiveCaasFileHelper.Setup(x => x.SerializeParquetFile(It.IsAny<List<Cohort>>(), _cohort, _blobName, 1));
        // Act
        await _receiveCaasFileInstance.Run(fileSteam, _blobName);

            // Act
            await _receiveCaasFileInstance.Run(fileSteam, _blobName);

        var response = MockHelpers.CreateMockHttpResponseData(HttpStatusCode.OK);
        _mockICallFunction.Setup(call => call.SendPost(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult(response));
    }

    [TestMethod]
    public async Task Run_SuccessfulParseWithInvalidInput_FailsAndLogsError()
    {
        // Arrange
        var _invalidfile = "BSS_20241201121212_n30.parquet";
        await using var fileSteam = File.OpenRead(_blobName);
        _mockICallFunction.Setup(callFunction => callFunction.SendPost(It.IsAny<string>(), It.IsAny<string>())).Verifiable();
        _screeningServiceData.Setup(x => x.GetScreeningServiceByAcronym(It.IsAny<string>())).Returns(new ScreeningService());
        _mockIReceiveCaasFileHelper.Setup(x => x.InitialChecks(fileSteam, _invalidfile)).Returns(Task.FromResult(true));
        _mockIReceiveCaasFileHelper.Setup(x => x.GetNumberOfRecordsFromFileName(_invalidfile)).Returns(Task.FromResult<int?>(30));
        _mockIReceiveCaasFileHelper.Setup(x => x.MapParticipant(_participantsParquetMap, _participant, _invalidfile, It.IsAny<int>())).Returns(Task.FromResult(It.IsAny<Participant?>()));
        _mockIReceiveCaasFileHelper.Setup(x => x.SerializeParquetFile(It.IsAny<List<Cohort>>(), _cohort, It.IsAny<string>(), It.IsAny<int>()));
        // Act
        await _receiveCaasFileInstance.Run(fileSteam, _invalidfile);

        // Assert
        _mockLogger.Verify(x => x.Log(It.Is<LogLevel>(l => l == LogLevel.Error),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Invalid data in the file: BSS_20241201121212_n30.parquet")),
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
            Times.Never);
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

        await using var fileSteam = File.OpenRead(_blobName);
        _mockICallFunction.Setup(callFunction => callFunction.SendPost(It.IsAny<string>(), It.IsAny<string>())).Verifiable();
        _screeningServiceData.Setup(x => x.GetScreeningServiceByAcronym(It.IsAny<string>())).Returns(new ScreeningService());
        _mockIReceiveCaasFileHelper.Setup(x => x.InitialChecks(fileSteam, fileName)).Returns(Task.FromResult(false));

        // Act & Assert
        await _receiveCaasFileInstance.Run(fileSteam, fileName);

        _mockLogger.Verify(x => x.Log(It.Is<LogLevel>(l => l == LogLevel.Error),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Invalid File.")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);

        _mockICallFunction.Verify(
            x => x.SendPost(It.Is<string>(url => url == "FileValidationURL"),
                It.IsAny<string>()),
            Times.Never);

        _mockICallFunction.Verify(
            x => x.SendPost(It.IsAny<string>(),
                It.Is<string>(json => json == _expectedJson)),
            Times.Never);
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

        await using var fileSteam = File.OpenRead(_blobName);
        _mockICallFunction.Setup(callFunction => callFunction.SendPost(It.IsAny<string>(), It.IsAny<string>())).Verifiable();
        _screeningServiceData.Setup(x => x.GetScreeningServiceByAcronym(It.IsAny<string>())).Returns(new ScreeningService());
        _mockIReceiveCaasFileHelper.Setup(x => x.InitialChecks(fileSteam, _blobName)).Returns(Task.FromResult(true));
        _mockIReceiveCaasFileHelper.Setup(x => x.GetNumberOfRecordsFromFileName(_blobName)).Returns(Task.FromResult<int?>(1));
        _mockIReceiveCaasFileHelper.Setup(x => x.MapParticipant(_participantsParquetMap, new Participant(), _blobName, 1)).Returns(Task.FromResult<Participant?>(_participant));
        _mockIReceiveCaasFileHelper.Setup(x => x.SerializeParquetFile(It.IsAny<List<Cohort>>(), _cohort, _blobName, 1));
        // Act
        await _receiveCaasFileInstance.Run(fileSteam, _blobName);

        // Assert
        _mockICallFunction.Verify(
            x => x.SendPost(It.IsAny<string>(),
            It.Is<string>(json => json == _expectedJson)),
            Times.Never);
    }
}
