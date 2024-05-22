namespace NHS.CohortManager.Tests.CaasIntegrationTests;

using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NHS.Screening.ReceiveCaasFile;
using Common;

[TestClass]
public class ReceiveCaasFileTests
{
    private Mock<ILogger<ReceiveCaasFile>> mockLogger;
    private Mock<ICallFunction> mockICallFunction;
    private Mock<IFileReader> mockIFileReader;
    private ReceiveCaasFile receiveCaasFileInstance;
    private string validCsvData;
    private string invalidCsvData;
    private string expectedJson;
    private string blobName;

    [TestInitialize]
    public void Setup()
    {
        mockLogger = new Mock<ILogger<ReceiveCaasFile>>();
        mockICallFunction = new Mock<ICallFunction>();
        mockIFileReader = new Mock<IFileReader>();
        receiveCaasFileInstance = new ReceiveCaasFile(mockLogger.Object, mockICallFunction.Object);
        blobName = "testBlob";

       validCsvData = "record_type,change_time_stamp,serial_change_number,nhs_number,superseded_by_nhs_number,primary_care_provider,primary_care_provider_business_effective_from_date,current_posting,current_posting_business_effective_from_date,previous_posting,previous_posting_business_effective_from_date,name_prefix,given_name,other_given_name(s),family_name,previous_family_name,date_of_birth,gender,address_line_1,address_line_2,address_line_3,address_line_4,address_line_5,postcode,Paf_key,usual_address_business_effective_from_date,reason_for_removal,reason_for_removal_business_effective_from_date,date_of_death,death_status,telephone_number(home),telephone_number(home)_business_effective_from_date,telephone_number(mobile),telephone_number(mobile)_business_effective_from_date,email_address(home),email_address(home)_business_effective_from_date,preferred_language,is_interpreter_required,invalid_flag,record_identifier,change_reason_code\n" +
                    "New,20240524153000,1,1111111111,null,B83006,10/04/2024,Manchester,10/04/2024,Edinburgh,10/04/2023,Mr,Joe,null,Bloggs,null,21/12/1971,1,HEXAGON HOUSE,PYNES HILL,RYDON LANE,EXETER,DEVON,BV3 9ZA,1234,null,null,null,null,null,null,null,null,null,null,null,English,0,0,1,null\n" +
                    "Amended,20240524153000,2,2222222222,null,D81026,11/04/2024,Liverpool,11/04/2024,Birmingham,11/04/2023,Mrs,Jane,null,Doe,null,01/08/1968,2,1 New Road,SOLIHULL,West Midlands,null,null,B91 3DL,4321,null,null,null,01/05/2024,1,null,null,null,null,null,null,English,0,0,2,null\n" +
                    "Removed,20240524153000,3,3333333333,null,L83137,12/04/2024,London,12/04/2024,Swansea,12/04/2023,Dr,John,null,Jones,null,01/12/1950,1,100,spen lane,Leeds,null,null,LS16 5BR,5555,null,null,null,null,null,null,null,null,null,null,null,French,1,0,3,null\n";
        invalidCsvData = "invalid data";
    }

    [TestMethod]
    public async Task Run_SuccessfulParseAndSendDataWithValidInput_SuccessfulSendToFunctionWithResponse()
    {
        // Arrange
        byte[] csvDataBytes = Encoding.UTF8.GetBytes(validCsvData);
        var memoryStream = new MemoryStream(csvDataBytes);
        mockIFileReader.Setup(fileReader => fileReader.ReadStream(It.IsAny<Stream>()))
                        .Returns(() => new StreamReader(memoryStream));

        mockICallFunction.Setup(callFunction => callFunction.SendPost(It.IsAny<string>(), It.IsAny<string>())).Verifiable();
        expectedJson = "{\"Participants\":[{\"RecordType\":\"New\",\"ChangeTimeStamp\":\"20240524153000\",\"SerialChangeNumber\":\"1\",\"NHSId\":\"1111111111\",\"SupersededByNhsNumber\":\"null\",\"PrimaryCareProvider\":\"B83006\",\"PrimaryCareProviderEffectiveFrom\":\"10/04/2024\",\"CurrentPosting\":\"Manchester\",\"CurrentPostingEffectiveFrom\":\"10/04/2024\",\"PreviousPosting\":\"Edinburgh\",\"PreviousPostingEffectiveFrom\":\"10/04/2023\",\"NamePrefix\":\"Mr\",\"FirstName\":\"Joe\",\"OtherGivenNames\":\"null\",\"Surname\":\"Bloggs\",\"PreviousSurname\":\"null\",\"DateOfBirth\":\"21/12/1971\",\"Gender\":1,\"AddressLine1\":\"HEXAGON HOUSE\",\"AddressLine2\":\"PYNES HILL\",\"AddressLine3\":\"RYDON LANE\",\"AddressLine4\":\"EXETER\",\"AddressLine5\":\"DEVON\",\"Postcode\":\"BV3 9ZA\",\"PafKey\":\"1234\",\"UsualAddressEffectiveFromDate\":\"null\",\"ReasonForRemoval\":\"null\",\"ReasonForRemovalEffectiveFromDate\":\"null\",\"DateOfDeath\":\"null\",\"DeathStatus\":null,\"TelephoneNumber\":\"null\",\"TelephoneNumberEffectiveFromDate\":\"null\",\"MobileNumber\":\"null\",\"MobileNumberEffectiveFromDate\":\"null\",\"EmailAddress\":\"null\",\"EmailAddressEffectiveFromDate\":\"null\",\"PreferredLanguage\":\"English\",\"IsInterpreterRequired\":\"0\",\"InvalidFlag\":\"0\",\"RecordIdentifier\":\"1\",\"ChangeReasonCode\":\"null\"},{\"RecordType\":\"Amended\",\"ChangeTimeStamp\":\"20240524153000\",\"SerialChangeNumber\":\"2\",\"NHSId\":\"2222222222\",\"SupersededByNhsNumber\":\"null\",\"PrimaryCareProvider\":\"D81026\",\"PrimaryCareProviderEffectiveFrom\":\"11/04/2024\",\"CurrentPosting\":\"Liverpool\",\"CurrentPostingEffectiveFrom\":\"11/04/2024\",\"PreviousPosting\":\"Birmingham\",\"PreviousPostingEffectiveFrom\":\"11/04/2023\",\"NamePrefix\":\"Mrs\",\"FirstName\":\"Jane\",\"OtherGivenNames\":\"null\",\"Surname\":\"Doe\",\"PreviousSurname\":\"null\",\"DateOfBirth\":\"01/08/1968\",\"Gender\":2,\"AddressLine1\":\"1 New Road\",\"AddressLine2\":\"SOLIHULL\",\"AddressLine3\":\"West Midlands\",\"AddressLine4\":\"null\",\"AddressLine5\":\"null\",\"Postcode\":\"B91 3DL\",\"PafKey\":\"4321\",\"UsualAddressEffectiveFromDate\":\"null\",\"ReasonForRemoval\":\"null\",\"ReasonForRemovalEffectiveFromDate\":\"null\",\"DateOfDeath\":\"01/05/2024\",\"DeathStatus\":1,\"TelephoneNumber\":\"null\",\"TelephoneNumberEffectiveFromDate\":\"null\",\"MobileNumber\":\"null\",\"MobileNumberEffectiveFromDate\":\"null\",\"EmailAddress\":\"null\",\"EmailAddressEffectiveFromDate\":\"null\",\"PreferredLanguage\":\"English\",\"IsInterpreterRequired\":\"0\",\"InvalidFlag\":\"0\",\"RecordIdentifier\":\"2\",\"ChangeReasonCode\":\"null\"},{\"RecordType\":\"Removed\",\"ChangeTimeStamp\":\"20240524153000\",\"SerialChangeNumber\":\"3\",\"NHSId\":\"3333333333\",\"SupersededByNhsNumber\":\"null\",\"PrimaryCareProvider\":\"L83137\",\"PrimaryCareProviderEffectiveFrom\":\"12/04/2024\",\"CurrentPosting\":\"London\",\"CurrentPostingEffectiveFrom\":\"12/04/2024\",\"PreviousPosting\":\"Swansea\",\"PreviousPostingEffectiveFrom\":\"12/04/2023\",\"NamePrefix\":\"Dr\",\"FirstName\":\"John\",\"OtherGivenNames\":\"null\",\"Surname\":\"Jones\",\"PreviousSurname\":\"null\",\"DateOfBirth\":\"01/12/1950\",\"Gender\":1,\"AddressLine1\":\"100\",\"AddressLine2\":\"spen lane\",\"AddressLine3\":\"Leeds\",\"AddressLine4\":\"null\",\"AddressLine5\":\"null\",\"Postcode\":\"LS16 5BR\",\"PafKey\":\"5555\",\"UsualAddressEffectiveFromDate\":\"null\",\"ReasonForRemoval\":\"null\",\"ReasonForRemovalEffectiveFromDate\":\"null\",\"DateOfDeath\":\"null\",\"DeathStatus\":null,\"TelephoneNumber\":\"null\",\"TelephoneNumberEffectiveFromDate\":\"null\",\"MobileNumber\":\"null\",\"MobileNumberEffectiveFromDate\":\"null\",\"EmailAddress\":\"null\",\"EmailAddressEffectiveFromDate\":\"null\",\"PreferredLanguage\":\"French\",\"IsInterpreterRequired\":\"1\",\"InvalidFlag\":\"0\",\"RecordIdentifier\":\"3\",\"ChangeReasonCode\":\"null\"}]}";
        // Act
        await receiveCaasFileInstance.Run(memoryStream, blobName);

        // Assert
        mockLogger.Verify(
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

        mockICallFunction.Verify(
            x => x.SendPost(It.IsAny<string>(),
            It.Is<string>(json => json == expectedJson)),
            Times.Once);
    }

    [TestMethod]
    public async Task Run_SuccessfulParseWithInvalidInput_FailsAndLogsError()
    {
        // Arrange
        byte[] csvDataBytes = Encoding.UTF8.GetBytes(invalidCsvData);
        var memoryStream = new MemoryStream(csvDataBytes);
        mockIFileReader.Setup(fileReader => fileReader.ReadStream(It.IsAny<Stream>()))
                        .Returns(() => new StreamReader(memoryStream));

        mockICallFunction.Setup(callFunction => callFunction.SendPost(It.IsAny<string>(), It.IsAny<string>())).Verifiable();

        // Act
        await receiveCaasFileInstance.Run(memoryStream, blobName);

        //Assert
        mockLogger.Verify(
                x => x.Log(It.Is<LogLevel>(l => l == LogLevel.Information),
                            It.IsAny<EventId>(),
                            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Created 0 Objects")),
                            It.IsAny<Exception>(),
                            It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                            Times.Once);

        mockLogger.Verify(
                x => x.Log(It.Is<LogLevel>(l => l == LogLevel.Information),
                            It.IsAny<EventId>(),
                            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to create 0 Objects")),
                            It.IsAny<Exception>(),
                            It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                            Times.Once);
    }

    [TestMethod]
    public async Task Run_UnsuccessfulParseAndSendDataWithValidInput_FailsAndLogsError()
    {
        // Arrange
        byte[] csvDataBytes = Encoding.UTF8.GetBytes(validCsvData);
        var memoryStream = new MemoryStream(csvDataBytes);
        mockIFileReader.Setup(fileReader => fileReader.ReadStream(It.IsAny<Stream>()))
                        .Throws(new Exception("Failed to read the incoming file"));
        mockICallFunction.Setup(callFunction => callFunction.SendPost(It.IsAny<string>(), It.IsAny<string>())).Verifiable();

        // Act
        await receiveCaasFileInstance.Run(memoryStream, blobName);

        // Assert
        mockLogger.Verify(l => l.Log(LogLevel.Information,
                                        It.IsAny<EventId>(),
                                        It.IsAny<It.IsAnyType>(),
                                        It.IsAny<Exception>(),
                                        It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                                        Times.AtLeastOnce());
    }
}
