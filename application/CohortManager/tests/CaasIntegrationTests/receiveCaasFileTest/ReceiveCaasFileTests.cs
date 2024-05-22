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

        // csvData
        validCsvData = "nhs_number,superseded_by_nhs_number,primary_care_provider,gp_connect,name_prefix,given_name,other_given_names,family_name,date_of_birth,gender,address_line_1,address_line_2,address_line_3,address_line_4,address_line_5,postcode,reason_for_removal,reason_for_removal_effective_from_date,date_of_death,telephone_number,mobile_number,email_address,preferred_language,is_interpreter_required,action\n" +
                        "1111111111,null,B83006,TRUE,Mr,Joe,null,Bloggs,21/12/1971,1,HEXAGON HOUSE,PYNES HILL,RYDON LANE,EXETER,DEVON,BV3 9ZA,null,null,null,null,null,null,null,null,UPDATE\n" +
                        "2222222222,null,D81026,TRUE,Mrs,Jane,null,Doe,01/08/1968,2,1 New Road,SOLIHULL,West Midlands,null,null,B91 3DL,null,null,01/08/1968,null,null,null,null,null,ADD\n" +
                        "3333333333,null,L83137,TRUE,Dr,john,null,jones,01/12/1950,1,100,spen lane,Leeds,null,null,LS16 5BR,null,null,null,null,null,null,null,null,DEL\n";
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
        expectedJson = "{\"Cohort\":[{\"NHSId\":\"1111111111\",\"SupersededByNhsNumber\":\"null\",\"PrimaryCareProvider\":\"B83006\",\"GpConnect\":\"TRUE\",\"NamePrefix\":\"Mr\",\"FirstName\":\"Joe\",\"OtherGivenNames\":\"null\",\"Surname\":\"Bloggs\",\"DateOfBirth\":\"21/12/1971\",\"Gender\":\"1\",\"AddressLine1\":\"HEXAGON HOUSE\",\"AddressLine2\":\"PYNES HILL\",\"AddressLine3\":\"RYDON LANE\",\"AddressLine4\":\"EXETER\",\"AddressLine5\":\"DEVON\",\"Postcode\":\"BV3 9ZA\",\"ReasonForRemoval\":\"null\",\"ReasonForRemovalEffectiveFromDate\":\"null\",\"DateOfDeath\":\"null\",\"TelephoneNumber\":\"null\",\"MobileNumber\":\"null\",\"EmailAddress\":\"null\",\"PreferredLanguage\":\"null\",\"IsInterpreterRequired\":\"null\",\"Action\":\"UPDATE\"},{\"NHSId\":\"2222222222\",\"SupersededByNhsNumber\":\"null\",\"PrimaryCareProvider\":\"D81026\",\"GpConnect\":\"TRUE\",\"NamePrefix\":\"Mrs\",\"FirstName\":\"Jane\",\"OtherGivenNames\":\"null\",\"Surname\":\"Doe\",\"DateOfBirth\":\"01/08/1968\",\"Gender\":\"2\",\"AddressLine1\":\"1 New Road\",\"AddressLine2\":\"SOLIHULL\",\"AddressLine3\":\"West Midlands\",\"AddressLine4\":\"null\",\"AddressLine5\":\"null\",\"Postcode\":\"B91 3DL\",\"ReasonForRemoval\":\"null\",\"ReasonForRemovalEffectiveFromDate\":\"null\",\"DateOfDeath\":\"01/08/1968\",\"TelephoneNumber\":\"null\",\"MobileNumber\":\"null\",\"EmailAddress\":\"null\",\"PreferredLanguage\":\"null\",\"IsInterpreterRequired\":\"null\",\"Action\":\"ADD\"},{\"NHSId\":\"3333333333\",\"SupersededByNhsNumber\":\"null\",\"PrimaryCareProvider\":\"L83137\",\"GpConnect\":\"TRUE\",\"NamePrefix\":\"Dr\",\"FirstName\":\"john\",\"OtherGivenNames\":\"null\",\"Surname\":\"jones\",\"DateOfBirth\":\"01/12/1950\",\"Gender\":\"1\",\"AddressLine1\":\"100\",\"AddressLine2\":\"spen lane\",\"AddressLine3\":\"Leeds\",\"AddressLine4\":\"null\",\"AddressLine5\":\"null\",\"Postcode\":\"LS16 5BR\",\"ReasonForRemoval\":\"null\",\"ReasonForRemovalEffectiveFromDate\":\"null\",\"DateOfDeath\":\"null\",\"TelephoneNumber\":\"null\",\"MobileNumber\":\"null\",\"EmailAddress\":\"null\",\"PreferredLanguage\":\"null\",\"IsInterpreterRequired\":\"null\",\"Action\":\"DEL\"}]}";
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
