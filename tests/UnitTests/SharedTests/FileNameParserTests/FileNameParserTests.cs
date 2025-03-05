namespace NHS.CohortManager.Tests.UnitTests.FileNameParserTests;

using NHS.Screening.ReceiveCaasFile;

[TestClass]
public class FileNameParserTests
{
    private const string _validFileName = "202411261720028838554_1B8F53_-_CAAS_BREAST_SCREENING_COHORT.parquet";
    private const string _invalidFileName = "test.csv";

    [TestMethod]
    [DataRow(_validFileName, true)]
    [DataRow(_invalidFileName, false)]
    public void Run_FileNameParserIsValid_ReturnsBoolean(string fileName, bool expected)
    {
        // Arrange & Act
        var result = new FileNameParser(fileName);

        // Assert
        Assert.AreEqual(expected, result.IsValid);
    }

    [TestMethod]
    [DataRow(_validFileName, "CAAS_BREAST_SCREENING_COHORT")]
    [DataRow(_invalidFileName, "")]
    public void Run_FileNameParserGetScreeningService_ReturnsString(string fileName, string expected)
    {
        // Arrange & Act
        var result = new FileNameParser(fileName);

        // Assert
        Assert.AreEqual(expected, result.GetScreeningService());
    }
}
