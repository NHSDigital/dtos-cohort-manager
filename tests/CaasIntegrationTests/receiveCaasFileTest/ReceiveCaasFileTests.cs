namespace NHS.CohortManager.Tests.CaasIntegrationTests;

using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Common;
using NHS.Screening.ReceiveCaasFile;

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

    public ReceiveCaasFileTests()
    {
        _mockLogger = new Mock<ILogger<ReceiveCaasFile>>();
        _mockICallFunction = new Mock<ICallFunction>();
        _mockIFileReader = new Mock<IFileReader>();
        _receiveCaasFileInstance = new ReceiveCaasFile(_mockLogger.Object, _mockICallFunction.Object);
        _blobName = "testBlob";

        _validCsvData = "Record Type,Change Time Stamp,Serial Change Number,NHS Number,Superseded by NHS number,Primary Care Provider ,Primary Care Provider Business Effective From Date,Current Posting,Current Posting Business Effective From Date,Previous Posting,Previous Posting Business Effective To Date,Name Prefix,Given Name ,Other Given Name(s) ,Family Name ,Previous Family Name ,Date of Birth,Gender,Address line 1,Address line 2,Address line 3,Address line 4,Address line 5,Postcode,PAF key,Usual Address Business Effective From Date,Reason for Removal,Reason for Removal Business Effective From Date,Date of Death,Death Status,Telephone Number (Home),Telephone Number (Home) Business Effective From Date,Telephone Number (Mobile),Telephone Number (Mobile) Business Effective From Date,E-mail address (Home),E-mail address (Home) Business Effective From Date,Preferred Language,Interpreter required,Invalid Flag,Record Identifier,Change Reason Code\n" +
        "New,20240524153000,1,1111111111,,B83006,20240410,Manchester,20240410,Edinburgh,20230410,Mr,Joe,,Bloggs,,19711221,1,HEXAGON HOUSE,PYNES HILL,RYDON LANE,EXETER,DEVON,BV3 9ZA,1234,,,,,,,,,,,,English,0,0,1,,\n" +
        "Amended,20240524153000,2,2222222222,,D81026,20240411,Liverpool,20240411,Birmingham,20230411,Mrs,Jane,,Doe,,19680801,2,1 New Road,SOLIHULL,West Midlands,,,B91 3DL,4321,,,,20240501,1,,,,,,,English,0,0,2,,\n" +
        "Removed,20240524153000,3,3333333333,,L83137,20240412,London,20240412,Swansea,20230412,Dr,John,,Jones,,19501201,1,100,spen lane,Leeds,,,LS16 5BR,5555,,,,,,,,,,,,French,1,0,3,,";

        _invalidCsvData = "invalid data";
    }

    [TestMethod]
    public async Task Run_SuccessfulParseAndSendDataWithValidInput_SuccessfulSendToFunctionWithResponse()
    {
        // Arrange
        byte[] csvDataBytes = Encoding.UTF8.GetBytes(_validCsvData);
        var memoryStream = new MemoryStream(csvDataBytes);
        _mockIFileReader.Setup(fileReader => fileReader.ReadStream(It.IsAny<Stream>()))
                        .Returns(() => new StreamReader(memoryStream));

        _mockICallFunction.Setup(callFunction => callFunction.SendPost(It.IsAny<string>(), It.IsAny<string>())).Verifiable();
        _expectedJson = "{\"Participants\":[{\"RecordType\":\"New\",\"ChangeTimeStamp\":\"20240524153000\",\"SerialChangeNumber\":\"1\",\"NHSId\":\"1111111111\",\"SupersededByNhsNumber\":\"\",\"PrimaryCareProvider\":\"B83006\",\"PrimaryCareProviderEffectiveFromDate\":\"20240410\",\"CurrentPosting\":\"Manchester\",\"CurrentPostingEffectiveFromDate\":\"20240410\",\"PreviousPosting\":\"Edinburgh\",\"PreviousPostingEffectiveFromDate\":\"20230410\",\"NamePrefix\":\"Mr\",\"FirstName\":\"Joe\",\"OtherGivenNames\":\"\",\"Surname\":\"Bloggs\",\"PreviousSurname\":\"\",\"DateOfBirth\":\"19711221\",\"Gender\":1,\"AddressLine1\":\"HEXAGON HOUSE\",\"AddressLine2\":\"PYNES HILL\",\"AddressLine3\":\"RYDON LANE\",\"AddressLine4\":\"EXETER\",\"AddressLine5\":\"DEVON\",\"Postcode\":\"BV3 9ZA\",\"PafKey\":\"1234\",\"UsualAddressEffectiveFromDate\":\"\",\"ReasonForRemoval\":\"\",\"ReasonForRemovalEffectiveFromDate\":\"\",\"DateOfDeath\":\"\",\"DeathStatus\":null,\"TelephoneNumber\":\"\",\"TelephoneNumberEffectiveFromDate\":\"\",\"MobileNumber\":\"\",\"MobileNumberEffectiveFromDate\":\"\",\"EmailAddress\":\"\",\"EmailAddressEffectiveFromDate\":\"\",\"PreferredLanguage\":\"English\",\"IsInterpreterRequired\":\"0\",\"InvalidFlag\":\"0\",\"RecordIdentifier\":\"1\",\"ChangeReasonCode\":\"\",\"ParticipantId\":null},{\"RecordType\":\"Amended\",\"ChangeTimeStamp\":\"20240524153000\",\"SerialChangeNumber\":\"2\",\"NHSId\":\"2222222222\",\"SupersededByNhsNumber\":\"\",\"PrimaryCareProvider\":\"D81026\",\"PrimaryCareProviderEffectiveFromDate\":\"20240411\",\"CurrentPosting\":\"Liverpool\",\"CurrentPostingEffectiveFromDate\":\"20240411\",\"PreviousPosting\":\"Birmingham\",\"PreviousPostingEffectiveFromDate\":\"20230411\",\"NamePrefix\":\"Mrs\",\"FirstName\":\"Jane\",\"OtherGivenNames\":\"\",\"Surname\":\"Doe\",\"PreviousSurname\":\"\",\"DateOfBirth\":\"19680801\",\"Gender\":2,\"AddressLine1\":\"1 New Road\",\"AddressLine2\":\"SOLIHULL\",\"AddressLine3\":\"West Midlands\",\"AddressLine4\":\"\",\"AddressLine5\":\"\",\"Postcode\":\"B91 3DL\",\"PafKey\":\"4321\",\"UsualAddressEffectiveFromDate\":\"\",\"ReasonForRemoval\":\"\",\"ReasonForRemovalEffectiveFromDate\":\"\",\"DateOfDeath\":\"20240501\",\"DeathStatus\":1,\"TelephoneNumber\":\"\",\"TelephoneNumberEffectiveFromDate\":\"\",\"MobileNumber\":\"\",\"MobileNumberEffectiveFromDate\":\"\",\"EmailAddress\":\"\",\"EmailAddressEffectiveFromDate\":\"\",\"PreferredLanguage\":\"English\",\"IsInterpreterRequired\":\"0\",\"InvalidFlag\":\"0\",\"RecordIdentifier\":\"2\",\"ChangeReasonCode\":\"\",\"ParticipantId\":null},{\"RecordType\":\"Removed\",\"ChangeTimeStamp\":\"20240524153000\",\"SerialChangeNumber\":\"3\",\"NHSId\":\"3333333333\",\"SupersededByNhsNumber\":\"\",\"PrimaryCareProvider\":\"L83137\",\"PrimaryCareProviderEffectiveFromDate\":\"20240412\",\"CurrentPosting\":\"London\",\"CurrentPostingEffectiveFromDate\":\"20240412\",\"PreviousPosting\":\"Swansea\",\"PreviousPostingEffectiveFromDate\":\"20230412\",\"NamePrefix\":\"Dr\",\"FirstName\":\"John\",\"OtherGivenNames\":\"\",\"Surname\":\"Jones\",\"PreviousSurname\":\"\",\"DateOfBirth\":\"19501201\",\"Gender\":1,\"AddressLine1\":\"100\",\"AddressLine2\":\"spen lane\",\"AddressLine3\":\"Leeds\",\"AddressLine4\":\"\",\"AddressLine5\":\"\",\"Postcode\":\"LS16 5BR\",\"PafKey\":\"5555\",\"UsualAddressEffectiveFromDate\":\"\",\"ReasonForRemoval\":\"\",\"ReasonForRemovalEffectiveFromDate\":\"\",\"DateOfDeath\":\"\",\"DeathStatus\":null,\"TelephoneNumber\":\"\",\"TelephoneNumberEffectiveFromDate\":\"\",\"MobileNumber\":\"\",\"MobileNumberEffectiveFromDate\":\"\",\"EmailAddress\":\"\",\"EmailAddressEffectiveFromDate\":\"\",\"PreferredLanguage\":\"French\",\"IsInterpreterRequired\":\"1\",\"InvalidFlag\":\"0\",\"RecordIdentifier\":\"3\",\"ChangeReasonCode\":\"\",\"ParticipantId\":null}],\"FileName\":\"testBlob\"}";

        // Act
        await _receiveCaasFileInstance.Run(memoryStream, _blobName);

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
            It.Is<string>(json => json == _expectedJson)),
            Times.Once);
    }

    [TestMethod]
    public async Task Run_SuccessfulParseWithInvalidInput_FailsAndLogsError()
    {
        // Arrange
        byte[] csvDataBytes = Encoding.UTF8.GetBytes(_invalidCsvData);
        var memoryStream = new MemoryStream(csvDataBytes);
        _mockIFileReader.Setup(fileReader => fileReader.ReadStream(It.IsAny<Stream>()))
        .Returns(() => new StreamReader(memoryStream));

        _mockICallFunction.Setup(callFunction => callFunction.SendPost(It.IsAny<string>(), It.IsAny<string>())).Verifiable();

        // Act
        await _receiveCaasFileInstance.Run(memoryStream, _blobName);

        // Assert
        _mockLogger.Verify(x => x.Log(It.Is<LogLevel>(l => l == LogLevel.Error),
        It.IsAny<EventId>(),
        It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to create 0 Objects")),
        It.IsAny<Exception>(),
        It.IsAny<Func<It.IsAnyType, Exception, string>>()),
        Times.Once);

        _mockICallFunction.Verify(
        x => x.SendPost(It.IsAny<string>(),
        It.IsAny<string>()),
        Times.Never);
    }

    [TestMethod]
    public async Task Run_UnsuccessfulParseAndSendDataWithValidInput_FailsAndLogsError()
    {
        // Arrange
        byte[] csvDataBytes = Encoding.UTF8.GetBytes(_validCsvData);
        var memoryStream = new MemoryStream(csvDataBytes);
        _mockIFileReader.Setup(fileReader => fileReader.ReadStream(It.IsAny<Stream>()))
                        .Throws(new Exception("Failed to read the incoming file"));
        _mockICallFunction.Setup(callFunction => callFunction.SendPost(It.IsAny<string>(), It.IsAny<string>())).Verifiable();

        // Act
        await _receiveCaasFileInstance.Run(memoryStream, _blobName);

        // Assert
        _mockLogger.Verify(l => l.Log(LogLevel.Information,
        It.IsAny<EventId>(),
        It.IsAny<It.IsAnyType>(),
        It.IsAny<Exception>(),
        It.IsAny<Func<It.IsAnyType, Exception, string>>()),
        Times.AtLeastOnce());
    }
}
